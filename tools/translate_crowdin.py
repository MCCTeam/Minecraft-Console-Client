#!/usr/bin/env python3
"""Translate Crowdin XLIFF bundles via Alibaba Cloud Qwen-MT API.

Workflow:
  1. Download a Crowdin bundle (or reuse an existing one)
  2. Parse XLIFF files, extract needs-translation entries
  3. Call Qwen-MT for each entry independently
  4. Generate per-language XLIFF with translated entries only
  5. Upload via `crowdin file upload --xliff`

Requires: Python 3.10+, crowdin CLI, ALI_BAILIAN_API_KEY env var.
No third-party Python packages needed (uses urllib for API calls).
"""

from __future__ import annotations

import argparse
import json
import logging
import os
import re
import subprocess
import sys
import textwrap
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from dataclasses import dataclass, field
from pathlib import Path

XLIFF_NS = "urn:oasis:names:tc:xliff:document:1.2"
NS = {"x": XLIFF_NS}

REPO_ROOT = Path(__file__).resolve().parent.parent
WORK_DIR = REPO_ROOT / ".crowdin-translate"
BUNDLES_DIR = WORK_DIR / "bundles"
DEFAULT_OUTPUT_DIR = WORK_DIR / "translated"
ERRORS_DIR = WORK_DIR / "errors"
DOMAIN_PROMPT_CACHE_DIR = WORK_DIR / "domain-prompt-cache"

QWEN_MT_API_URL = os.environ.get(
    "QWEN_MT_API_URL",
    "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
)

DOMAIN_PROMPT = """The sentence is from Minecraft Console Client (MCC), a text-based client for Minecraft Java Edition. Content includes application UI strings, bot/automation configuration, internal commands, status messages, and user documentation covering inventory, terrain, entities, crafting, movement, server connection, and CLI/configuration topics.
When translating, prioritize official Minecraft in-game terminology. Where the player community has widely adopted different terms, prefer the more recognizable one. Translate into this Minecraft client-tool domain style."""

# Crowdin locale -> (Qwen-MT target_lang, Crowdin CLI -l id, extra domain note)
LANGUAGE_MAP: dict[str, tuple[str, str, str]] = {
    "af_ZA": ("Afrikaans",            "af",    ""),
    "ar_SA": ("Arabic",               "ar",    ""),
    "az_AZ": ("North Azerbaijani",    "az",    ""),
    "ca_ES": ("Catalan",              "ca",    ""),
    "cs_CZ": ("Czech",                "cs",    ""),
    "da_DK": ("Danish",               "da",    ""),
    "de_DE": ("German",               "de",    ""),
    "el_GR": ("Greek",                "el",    ""),
    "es_ES": ("Spanish",              "es-ES", ""),
    "fi_FI": ("Finnish",              "fi",    ""),
    "fr_FR": ("French",               "fr",    ""),
    "he_IL": ("Hebrew",               "he",    ""),
    "hi_IN": ("Hindi",                "hi",    ""),
    "hu_HU": ("Hungarian",            "hu",    ""),
    "id_ID": ("Indonesian",           "id",    ""),
    "it_IT": ("Italian",              "it",    ""),
    "ja_JP": ("Japanese",             "ja",    ""),
    "ko_KR": ("Korean",               "ko",    ""),
    "lv_LV": ("Latvian",              "lv",    ""),
    "nl_NL": ("Dutch",                "nl",    ""),
    "no_NO": ("Norwegian Bokmål",     "no",    ""),
    "pl_PL": ("Polish",               "pl",    ""),
    "pt_BR": ("Portuguese",           "pt-BR", "Translate into Brazilian Portuguese."),
    "pt_PT": ("Portuguese",           "pt-PT", "Translate into European Portuguese."),
    "ro_RO": ("Romanian",             "ro",    ""),
    "ru_RU": ("Russian",              "ru",    ""),
    "sr_SP": ("Serbian",              "sr",    ""),
    "sv_SE": ("Swedish",              "sv-SE", ""),
    "fil_PH": ("Tagalog",             "fil",   ""),
    "tr_TR": ("Turkish",              "tr",    ""),
    "uk_UA": ("Ukrainian",            "uk",    ""),
    "vi_VN": ("Vietnamese",           "vi",    ""),
    "zh_CN": ("Chinese",              "zh-CN", ""),
    "zh_TW": ("Traditional Chinese",  "zh-TW", ""),
}

# Locales with significant active users (based on usage analytics).
# Used as the default set when --languages is not specified.
# Pass --languages all to translate every locale in LANGUAGE_MAP.
DEFAULT_LOCALES: list[str] = [
    "zh_CN",  # CN  ~920
    "tr_TR",  # TR  ~380
    "de_DE",  # DE  ~190
    "pl_PL",  # PL  ~180
    "vi_VN",  # VN  ~160
    "hi_IN",  # IN  ~100
    "ru_RU",  # RU  ~100
    "fr_FR",  # FR  ~90
    "zh_TW",  # TW  ~80
    "nl_NL",  # NL  ~70
    "ja_JP",  # JP  ~60
    "pt_BR",  # BR  ~55
    "sv_SE",  # SE  ~45
    "fi_FI",  # FI  ~40
    "uk_UA",  # UA  ~35
    "id_ID",  # ID  ~30
    "it_IT",  # IT  ~25
    "fil_PH", # PH  ~20
]

log = logging.getLogger("translate_crowdin")


# ---------------------------------------------------------------------------
# Data structures
# ---------------------------------------------------------------------------

@dataclass
class FileInfo:
    file_id: str
    original: str
    source_language: str
    target_language: str
    project_id: str
    attrs: dict[str, str] = field(default_factory=dict)


@dataclass
class TransUnit:
    id: str
    source: str
    target_text: str
    context: str | None = None
    resname: str | None = None
    file_info: FileInfo | None = None
    translated: str | None = None


# ---------------------------------------------------------------------------
# XLIFF parsing
# ---------------------------------------------------------------------------

def parse_xliff(path: Path, *, exclude_paths: list[str] | None = None) -> list[TransUnit]:
    """Parse an XLIFF 1.2 file, return trans-units with state=needs-translation.

    exclude_paths: skip <file> elements whose ``original`` starts with any
    of these prefixes (e.g. ``["/docs/"]``).
    """
    tree = ET.parse(path)
    root = tree.getroot()
    units: list[TransUnit] = []

    for file_elem in root.findall(f"{{{XLIFF_NS}}}file"):
        original = file_elem.get("original", "")
        if exclude_paths and any(original.startswith(p) for p in exclude_paths):
            continue
        finfo = FileInfo(
            file_id=file_elem.get("id", ""),
            original=file_elem.get("original", ""),
            source_language=file_elem.get("source-language", "en"),
            target_language=file_elem.get("target-language", ""),
            project_id=file_elem.get("project-id", ""),
            attrs={k: v for k, v in file_elem.attrib.items()},
        )

        body = file_elem.find(f"{{{XLIFF_NS}}}body")
        if body is None:
            continue

        for tu in body.findall(f"{{{XLIFF_NS}}}trans-unit"):
            target_elem = tu.find(f"{{{XLIFF_NS}}}target")
            if target_elem is None or target_elem.get("state") != "needs-translation":
                continue

            source_elem = tu.find(f"{{{XLIFF_NS}}}source")
            source_text = source_elem.text or "" if source_elem is not None else ""
            target_text = target_elem.text or ""

            ctx = None
            cg = tu.find(f"{{{XLIFF_NS}}}context-group")
            if cg is not None:
                ctx_elem = cg.find(f"{{{XLIFF_NS}}}context")
                if ctx_elem is not None and ctx_elem.text:
                    ctx = ctx_elem.text.strip()

            units.append(TransUnit(
                id=tu.get("id", ""),
                source=source_text,
                target_text=target_text,
                context=ctx,
                resname=tu.get("resname"),
                file_info=finfo,
            ))

    return units


# ---------------------------------------------------------------------------
# Qwen-MT API
# ---------------------------------------------------------------------------

def call_qwen_mt(
    source_text: str,
    target_lang: str,
    model: str,
    api_key: str,
    context: str | None = None,
    extra_domain: str = "",
) -> str:
    """Call Qwen-MT translation API. Returns translated text."""
    domains = DOMAIN_PROMPT
    if extra_domain:
        domains += "\n" + extra_domain
    # if context:
    #     domains += f"\nText key: {context}"
    # domains += f"(THE ABOVE IS NOT CONTENT TO BE TRANSLATED!)"

    payload = {
        "model": model,
        "messages": [{"role": "user", "content": source_text}],
        "translation_options": {
            "source_lang": "English",
            "target_lang": target_lang,
            "domains": domains,
        },
    }

    data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(
        QWEN_MT_API_URL,
        data=data,
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}",
        },
        method="POST",
    )

    with urllib.request.urlopen(req, timeout=60) as resp:
        body = json.loads(resp.read().decode("utf-8"))

    return body["choices"][0]["message"]["content"]


# ---------------------------------------------------------------------------
# Domain-prompt leak detection
# ---------------------------------------------------------------------------

_LEAK_FINGERPRINTS_EN = [
    "cross-platform, text-based third-party client",
    "Mojang's localization for the target language",
    "keep the English name or translate descriptively",
    "Preserve all placeholders ({0})",
    "Translate into this Minecraft client-tool domain style",
    "command syntax (/command <arg>)",
    "bot/automation configuration, internal commands",
]


def _split_into_fragments(text: str, min_len: int = 6) -> list[str]:
    """Split translated domain prompt into sentence-level fragments."""
    raw = re.split(r'[。．\.\n！!？?；;：:\u3002]', text)
    fragments: list[str] = []
    for frag in raw:
        frag = frag.strip()
        if len(frag) >= min_len:
            fragments.append(frag)
    return fragments


def _char_ngrams(text: str, n: int = 5) -> set[str]:
    """Generate character n-grams from text (whitespace normalized)."""
    t = re.sub(r'\s+', '', text)
    return {t[i:i + n] for i in range(len(t) - n + 1)} if len(t) >= n else set()


def _shingle_similarity(reference_grams: set[str], candidate: str,
                        n: int = 5) -> float:
    """Fraction of reference n-grams found in candidate text."""
    if not reference_grams:
        return 0.0
    cand_grams = _char_ngrams(candidate, n)
    return len(reference_grams & cand_grams) / len(reference_grams)


def ensure_domain_prompt_cached(
    locale: str,
    target_lang: str,
    api_key: str,
    model: str,
) -> tuple[str, list[str]]:
    """Translate DOMAIN_PROMPT into target language, cache it, return (full_text, fragments).

    On subsequent runs the cached file is reused without an API call.
    """
    DOMAIN_PROMPT_CACHE_DIR.mkdir(parents=True, exist_ok=True)
    cache_file = DOMAIN_PROMPT_CACHE_DIR / f"{locale}.txt"

    if cache_file.exists():
        text = cache_file.read_text(encoding="utf-8")
        log.info("  Loaded cached domain prompt translation for %s", locale)
    else:
        log.info("  Translating domain prompt into %s for leak detection ...",
                 target_lang)
        text = call_qwen_mt(
            source_text=DOMAIN_PROMPT,
            target_lang=target_lang,
            model=model,
            api_key=api_key,
        )
        cache_file.write_text(text, encoding="utf-8")
        log.info("  Cached domain prompt translation -> %s", cache_file)

    return text, _split_into_fragments(text)


class DomainLeakDetector:
    """Detect and clean translations that contain leaked domain-prompt text.

    Uses the original English fingerprints plus per-language fragments
    obtained by translating the domain prompt itself.  A character n-gram
    (shingling) similarity check catches paraphrased leaks that exact
    substring matching would miss.
    """

    NGRAM_SIZE = 5
    FULL_TEXT_THRESHOLD = 0.25
    LINE_THRESHOLD = 0.35

    def __init__(self, cached_fragments: list[str] | None = None,
                 cached_full_text: str = ""):
        self._en = list(_LEAK_FINGERPRINTS_EN)
        self._translated = cached_fragments or []
        self._full_text = cached_full_text
        self._prompt_grams = _char_ngrams(cached_full_text, self.NGRAM_SIZE)
        self._fragment_grams = [
            _char_ngrams(f, self.NGRAM_SIZE) for f in self._translated
        ]

    def detect(self, source: str, translated: str) -> bool:
        for fp in self._en:
            if fp in translated and fp not in source:
                return True
        for fp in self._translated:
            if fp in translated and fp not in source:
                return True
        if self._prompt_grams:
            sim = _shingle_similarity(self._prompt_grams, translated,
                                      self.NGRAM_SIZE)
            if sim > self.FULL_TEXT_THRESHOLD:
                return True
        for fg in self._fragment_grams:
            if fg and _shingle_similarity(fg, translated, self.NGRAM_SIZE) > self.LINE_THRESHOLD:
                return True
        return False

    def _is_leak_line(self, line: str, source: str) -> bool:
        all_fps = self._en + self._translated
        if any(fp in line for fp in all_fps if fp not in source):
            return True
        if len(line.strip()) <= 10:
            return False
        for fg in self._fragment_grams:
            if fg and _shingle_similarity(fg, line, self.NGRAM_SIZE) > self.LINE_THRESHOLD:
                return True
        return False

    def postprocess(self, source: str, translated: str) -> str | None:
        """Return cleaned translation, or None if unsalvageable."""
        if not self.detect(source, translated):
            return translated

        lines = translated.split("\n")
        clean = [ln for ln in lines if not self._is_leak_line(ln, source)]
        cleaned = "\n".join(clean).strip()
        if not cleaned or len(cleaned) < max(len(source) * 0.2, 1):
            return None
        if self.detect(source, cleaned):
            return None
        return cleaned


# ---------------------------------------------------------------------------
# Rate-limited translator
# ---------------------------------------------------------------------------

MAX_RETRIES = 6
INITIAL_BACKOFF = 2.0  # seconds


class RateLimitedTranslator:
    """Single-threaded translator with strict RPM pacing and 429 retry."""

    def __init__(self, api_key: str, model: str, rpm: int, target_lang: str,
                 extra_domain: str = "",
                 leak_detector: DomainLeakDetector | None = None):
        self.api_key = api_key
        self.model = model
        self.rpm = rpm
        self.target_lang = target_lang
        self.extra_domain = extra_domain
        self.detector = leak_detector or DomainLeakDetector()
        self._interval = 60.0 / rpm
        self._last_call = 0.0

    def _pace(self) -> None:
        """Sleep to enforce strict RPM spacing between requests."""
        now = time.monotonic()
        wait = self._interval - (now - self._last_call)
        if wait > 0:
            time.sleep(wait)
        self._last_call = time.monotonic()

    def translate_one(self, unit: TransUnit) -> TransUnit:
        """Translate a single TransUnit with rate limiting and retry on 429."""
        leak_retries = 0
        for attempt in range(MAX_RETRIES + 1):
            self._pace()
            try:
                result = call_qwen_mt(
                    source_text=unit.source,
                    target_lang=self.target_lang,
                    model=self.model,
                    api_key=self.api_key,
                    context=unit.context,
                    extra_domain=self.extra_domain,
                )
                cleaned = self.detector.postprocess(unit.source, result)
                if cleaned is None and leak_retries < 2:
                    leak_retries += 1
                    log.warning("Domain prompt leak in unit %s, retrying (%d/2)",
                                unit.id, leak_retries)
                    continue
                if cleaned is None:
                    log.warning("Domain prompt leak in unit %s persists after "
                                "retries, skipping", unit.id)
                    unit.translated = None
                    return unit
                result = cleaned
                leading = len(unit.source) - len(unit.source.lstrip(" "))
                if leading > 0 and not result.startswith(" " * leading):
                    result = " " * leading + result.lstrip(" ")
                unit.translated = result
                return unit
            except urllib.error.HTTPError as exc:
                if exc.code == 429 and attempt < MAX_RETRIES:
                    backoff = INITIAL_BACKOFF * (2 ** attempt)
                    log.warning("429 on unit %s, retry %d/%d after %.1fs",
                                unit.id, attempt + 1, MAX_RETRIES, backoff)
                    time.sleep(backoff)
                    self._last_call = time.monotonic()
                    continue
                log.warning("Failed to translate unit %s: %s", unit.id, exc)
                unit.translated = None
                return unit
            except Exception as exc:
                log.warning("Failed to translate unit %s: %s", unit.id, exc)
                unit.translated = None
                return unit
        return unit

    def translate_batch(self, units: list[TransUnit],
                        progress_callback=None) -> tuple[list[TransUnit], bool]:
        """Translate a list of units sequentially with strict RPM pacing.

        Returns (results, interrupted): results may be partial if the user
        pressed Ctrl-C.  The caller should still persist whatever was completed.
        """
        if not units:
            return units, False

        results: list[TransUnit] = []
        interrupted = False
        for i, u in enumerate(units):
            try:
                self.translate_one(u)
            except KeyboardInterrupt:
                log.warning("Ctrl-C during translation, finishing up...")
                interrupted = True
                break
            results.append(u)
            if progress_callback:
                progress_callback(i + 1, len(units))

        return results, interrupted


# ---------------------------------------------------------------------------
# XLIFF output generation
# ---------------------------------------------------------------------------

def generate_output_xliff(units: list[TransUnit], target_language_xliff: str) -> str:
    """Generate an XLIFF 1.2 string containing only successfully translated units."""
    translated = [u for u in units if u.translated]
    if not translated:
        return ""

    by_file: dict[str, list[TransUnit]] = {}
    for u in translated:
        key = u.file_info.file_id if u.file_info else "0"
        by_file.setdefault(key, []).append(u)

    root = ET.Element("xliff", {
        "version": "1.2",
        "xmlns": XLIFF_NS,
    })

    for file_id, file_units in by_file.items():
        ref = file_units[0].file_info
        if not ref:
            continue

        file_attrs = dict(ref.attrs)
        file_elem = ET.SubElement(root, "file", file_attrs)
        body = ET.SubElement(file_elem, "body")

        for u in file_units:
            tu_attrs: dict[str, str] = {"id": u.id}
            if u.resname:
                tu_attrs["resname"] = u.resname
            tu_elem = ET.SubElement(body, "trans-unit", tu_attrs)
            src = ET.SubElement(tu_elem, "source")
            src.text = u.source
            tgt = ET.SubElement(tu_elem, "target", {"state": "translated"})
            tgt.text = u.translated

    ET.indent(root, space="  ")
    xml_str = ET.tostring(root, encoding="unicode", xml_declaration=False)
    return '<?xml version="1.0" encoding="UTF-8"?>\n' + xml_str + "\n"


# ---------------------------------------------------------------------------
# Bundle download
# ---------------------------------------------------------------------------

def download_bundle(bundle_id: int) -> Path:
    """Download a Crowdin bundle, collect XLIFF files into the work directory.

    crowdin bundle download extracts XLIFF files directly into cwd (no zip,
    no subdirectory). We snapshot existing *.xliff before the download, then
    move only the newly appeared files into BUNDLES_DIR/<timestamp>/.
    """
    BUNDLES_DIR.mkdir(parents=True, exist_ok=True)

    existing_xliffs = set(REPO_ROOT.glob("MCC_FullBundle_*.xliff"))

    log.info("Downloading Crowdin bundle %d ...", bundle_id)
    result = subprocess.run(
        ["crowdin", "bundle", "download", str(bundle_id)],
        capture_output=True, text=True, cwd=REPO_ROOT,
    )
    if result.returncode != 0:
        log.error("crowdin bundle download failed:\n%s\n%s",
                  result.stdout, result.stderr)
        sys.exit(1)

    new_xliffs = sorted(
        set(REPO_ROOT.glob("MCC_FullBundle_*.xliff")) - existing_xliffs
    )

    if not new_xliffs:
        all_xliffs = sorted(REPO_ROOT.glob("MCC_FullBundle_*.xliff"))
        if all_xliffs:
            log.info("No new XLIFF files appeared; using %d existing file(s) "
                     "in repo root", len(all_xliffs))
            new_xliffs = all_xliffs
        else:
            log.error("No XLIFF files found after download. stdout:\n%s",
                      result.stdout)
            sys.exit(1)

    timestamp = time.strftime("%Y%m%d-%H%M%S")
    dest = BUNDLES_DIR / f"bundle-{timestamp}"
    dest.mkdir(parents=True, exist_ok=True)

    for src in new_xliffs:
        target = dest / src.name
        src.rename(target)
    log.info("Moved %d XLIFF file(s) to %s", len(new_xliffs), dest)

    return dest


def extract_bundle_zip(zip_path: Path) -> Path:
    """Extract an existing bundle ZIP, return the extracted directory."""
    BUNDLES_DIR.mkdir(parents=True, exist_ok=True)
    dest = BUNDLES_DIR / Path(zip_path).stem
    dest.mkdir(parents=True, exist_ok=True)
    log.info("Extracting %s -> %s", zip_path.name, dest)
    with zipfile.ZipFile(zip_path, "r") as zf:
        zf.extractall(dest)
    return dest


# ---------------------------------------------------------------------------
# Crowdin upload
# ---------------------------------------------------------------------------

def upload_xliff(xliff_path: Path, crowdin_lang: str) -> bool:
    """Upload a translated XLIFF to Crowdin."""
    log.info("Uploading %s for language %s ...", xliff_path.name, crowdin_lang)
    result = subprocess.run(
        ["crowdin", "file", "upload", str(xliff_path),
         "--xliff", "-l", crowdin_lang],
        capture_output=True, text=True, cwd=REPO_ROOT,
    )
    if result.returncode != 0:
        log.error("Upload failed for %s:\n%s\n%s",
                  crowdin_lang, result.stdout, result.stderr)
        return False
    log.info("Upload succeeded for %s", crowdin_lang)
    return True


# ---------------------------------------------------------------------------
# Resume support
# ---------------------------------------------------------------------------

def load_existing_translated_ids(xliff_path: Path) -> set[str]:
    """Read an existing output XLIFF, return the set of translated unit IDs."""
    if not xliff_path.exists():
        return set()
    try:
        tree = ET.parse(xliff_path)
        root = tree.getroot()
        ids = set()
        for tu in root.iter(f"{{{XLIFF_NS}}}trans-unit"):
            uid = tu.get("id")
            if uid:
                ids.add(uid)
        return ids
    except ET.ParseError:
        return set()


# ---------------------------------------------------------------------------
# Main orchestration
# ---------------------------------------------------------------------------

def find_xliff_files(bundle_dir: Path, locales: list[str] | None) -> dict[str, Path]:
    """Map Crowdin locale -> XLIFF path, preserving the order of *locales*.

    When locales is None (all languages) files are ordered by filename.
    """
    available: dict[str, Path] = {}
    for xliff_path in sorted(bundle_dir.glob("*.xliff")):
        name = xliff_path.stem
        for locale in LANGUAGE_MAP:
            if name.endswith(f"_{locale}"):
                available[locale] = xliff_path
                break

    if locales is None:
        return available

    return {loc: available[loc] for loc in locales if loc in available}


def process_language(
    locale: str,
    xliff_path: Path,
    api_key: str,
    model: str,
    rpm: int,
    output_dir: Path,
    limit: int | None,
    dry_run: bool,
    skip_upload: bool,
    exclude_paths: list[str] | None = None,
) -> None:
    """Full pipeline for one language."""
    lang_info = LANGUAGE_MAP.get(locale)
    if not lang_info:
        log.warning("No language mapping for %s, skipping", locale)
        return

    target_lang, crowdin_lang, extra_domain = lang_info
    log.info("=" * 60)
    log.info("Processing %s -> %s", locale, target_lang)

    units = parse_xliff(xliff_path, exclude_paths=exclude_paths)
    log.info("  Found %d needs-translation entries", len(units))

    if not units:
        log.info("  Nothing to translate, skipping")
        return

    output_file = output_dir / f"MCC_Translated_{locale}.xliff"
    already_done = load_existing_translated_ids(output_file)
    if already_done:
        before = len(units)
        units = [u for u in units if u.id not in already_done]
        log.info("  Resuming: %d already translated, %d remaining",
                 before - len(units), len(units))

    if limit is not None and limit < len(units):
        log.info("  Limiting to first %d entries (--limit)", limit)
        units = units[:limit]

    if dry_run:
        log.info("  [DRY RUN] Would translate %d entries", len(units))
        if units:
            log.info("  Sample source (id=%s): %.100s...", units[0].id,
                     units[0].source)
        return

    if not units:
        log.info("  All entries already translated")
        return

    cached_full, cached_fragments = ensure_domain_prompt_cached(
        locale, target_lang, api_key, model)
    log.info("  Leak detector loaded %d fragment(s) for %s",
             len(cached_fragments), locale)
    detector = DomainLeakDetector(cached_fragments, cached_full)

    translator = RateLimitedTranslator(
        api_key=api_key,
        model=model,
        rpm=rpm,
        target_lang=target_lang,
        extra_domain=extra_domain,
        leak_detector=detector,
    )

    def on_progress(done: int, total: int) -> None:
        if done % 5 == 0 or done == total:
            log.info("  [%s] %d/%d (%.0f%%)", locale, done, total,
                     done / total * 100)

    translated_units, interrupted = translator.translate_batch(
        units, progress_callback=on_progress)

    success = sum(1 for u in translated_units if u.translated)
    failed = sum(1 for u in translated_units if u.translated is None)
    log.info("  Translated: %d, Failed: %d%s", success, failed,
             " (interrupted)" if interrupted else "")

    if failed > 0:
        ERRORS_DIR.mkdir(parents=True, exist_ok=True)
        err_path = ERRORS_DIR / f"errors_{locale}.log"
        with open(err_path, "a", encoding="utf-8") as f:
            for u in translated_units:
                if u.translated is None:
                    f.write(f"id={u.id} resname={u.resname} "
                            f"source={u.source[:200]}\n")
        log.info("  Error details written to %s", err_path)

    if already_done and output_file.exists():
        existing_units = _parse_existing_output(output_file)
        all_units = existing_units + [u for u in translated_units if u.translated]
    else:
        all_units = [u for u in translated_units if u.translated]

    if not all_units:
        if interrupted:
            raise KeyboardInterrupt
        return

    target_language_xliff = xliff_path.stem.split("_", 2)[-1] if "_" in xliff_path.stem else locale
    xliff_content = generate_output_xliff(all_units, target_language_xliff)
    if xliff_content:
        output_dir.mkdir(parents=True, exist_ok=True)
        output_file.write_text(xliff_content, encoding="utf-8")
        log.info("  Written: %s (%d units)", output_file.name, len(all_units))

        if not skip_upload and not interrupted:
            upload_xliff(output_file, crowdin_lang)

    if interrupted:
        raise KeyboardInterrupt


def _parse_existing_output(path: Path) -> list[TransUnit]:
    """Re-parse a previously generated output XLIFF into TransUnit objects."""
    tree = ET.parse(path)
    root = tree.getroot()
    units: list[TransUnit] = []

    for file_elem in root.findall(f"{{{XLIFF_NS}}}file"):
        finfo = FileInfo(
            file_id=file_elem.get("id", ""),
            original=file_elem.get("original", ""),
            source_language=file_elem.get("source-language", "en"),
            target_language=file_elem.get("target-language", ""),
            project_id=file_elem.get("project-id", ""),
            attrs={k: v for k, v in file_elem.attrib.items()},
        )
        body = file_elem.find(f"{{{XLIFF_NS}}}body")
        if body is None:
            continue
        for tu in body.findall(f"{{{XLIFF_NS}}}trans-unit"):
            src_elem = tu.find(f"{{{XLIFF_NS}}}source")
            tgt_elem = tu.find(f"{{{XLIFF_NS}}}target")
            units.append(TransUnit(
                id=tu.get("id", ""),
                source=src_elem.text or "" if src_elem is not None else "",
                target_text="",
                resname=tu.get("resname"),
                file_info=finfo,
                translated=tgt_elem.text or "" if tgt_elem is not None else "",
            ))

    return units


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        description="Translate Crowdin XLIFF bundles using Qwen-MT API",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=textwrap.dedent("""\
            Examples:
              %(prog)s --dry-run
              %(prog)s --languages zh_CN,ja_JP --limit 10 --skip-upload
              %(prog)s --bundle-dir .crowdin-translate/bundles/bundle-xxx/
              %(prog)s --model qwen-mt-plus --rpm 30
        """),
    )
    src = p.add_mutually_exclusive_group()
    src.add_argument("--bundle-dir", type=Path, metavar="DIR",
                     help="Reuse an already-extracted bundle directory")
    src.add_argument("--bundle-zip", type=Path, metavar="ZIP",
                     help="Reuse an already-downloaded bundle ZIP")
    p.add_argument("--bundle-id", type=int, default=2,
                   help="Crowdin bundle ID to download (default: 2)")
    p.add_argument("-l", "--languages", type=str, default=None,
                   help="Comma-separated Crowdin locales (e.g. zh_CN,ja_JP), "
                        "'all' for every supported locale, or omit to use the "
                        "default active-user set")
    p.add_argument("--model", type=str, default="qwen-mt-plus",
                   choices=["qwen-mt-plus", "qwen-mt-flash", "qwen-mt-lite"],
                   help="Qwen-MT model (default: qwen-mt-plus)")
    p.add_argument("--rpm", type=int, default=60,
                   help="Max requests per minute (default: 60)")
    p.add_argument("--limit", type=int, default=None, metavar="N",
                   help="Translate at most N entries per language (for debugging)")
    p.add_argument("--dry-run", action="store_true",
                   help="Parse and report without calling the API")
    p.add_argument("--skip-upload", action="store_true",
                   help="Skip uploading translations to Crowdin")
    p.add_argument("--output-dir", type=Path, default=None,
                   help=f"Output directory (default: {DEFAULT_OUTPUT_DIR})")
    p.add_argument("--include-docs", action="store_true",
                   help="Include /docs/ files in translation (skipped by default)")
    p.add_argument("-v", "--verbose", action="store_true",
                   help="Enable debug logging")
    return p


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s [%(levelname)s] %(message)s",
        datefmt="%H:%M:%S",
    )

    api_key = os.environ.get("ALI_BAILIAN_API_KEY", "")
    if not api_key and not args.dry_run:
        log.error("ALI_BAILIAN_API_KEY environment variable is not set")
        sys.exit(1)

    if args.languages and args.languages.strip().lower() == "all":
        locales = None  # None means all locales in LANGUAGE_MAP
        log.info("Language selection: all %d supported locales", len(LANGUAGE_MAP))
    elif args.languages:
        locales = [s.strip() for s in args.languages.split(",")]
        unknown = [loc for loc in locales if loc not in LANGUAGE_MAP]
        if unknown:
            log.error("Unknown locale(s): %s\nAvailable: %s",
                      ", ".join(unknown), ", ".join(sorted(LANGUAGE_MAP)))
            sys.exit(1)
    else:
        locales = list(DEFAULT_LOCALES)
        log.info("Language selection: %d default locales (use --languages all for all)",
                 len(locales))

    if args.bundle_dir:
        bundle_dir = args.bundle_dir
        if not bundle_dir.is_dir():
            log.error("Bundle directory not found: %s", bundle_dir)
            sys.exit(1)
    elif args.bundle_zip:
        if not args.bundle_zip.is_file():
            log.error("Bundle ZIP not found: %s", args.bundle_zip)
            sys.exit(1)
        bundle_dir = extract_bundle_zip(args.bundle_zip)
    else:
        bundle_dir = download_bundle(args.bundle_id)

    output_dir = args.output_dir or DEFAULT_OUTPUT_DIR
    output_dir.mkdir(parents=True, exist_ok=True)

    exclude_paths: list[str] | None = None if args.include_docs else ["/docs/"]
    if exclude_paths:
        log.info("Excluding XLIFF files under: %s (use --include-docs to include)",
                 ", ".join(exclude_paths))

    xliff_files = find_xliff_files(bundle_dir, locales)
    if not xliff_files:
        log.error("No matching XLIFF files found in %s", bundle_dir)
        sys.exit(1)

    log.info("Found %d language(s) to process: %s",
             len(xliff_files), ", ".join(sorted(xliff_files)))

    for locale, xliff_path in xliff_files.items():
        try:
            process_language(
                locale=locale,
                xliff_path=xliff_path,
                api_key=api_key,
                model=args.model,
                rpm=args.rpm,
                output_dir=output_dir,
                limit=args.limit,
                dry_run=args.dry_run,
                skip_upload=args.skip_upload,
                exclude_paths=exclude_paths,
            )
        except KeyboardInterrupt:
            log.warning("Interrupted by user. Partial results have been saved.")
            sys.exit(130)
        except Exception:
            log.exception("Error processing %s", locale)


if __name__ == "__main__":
    main()
