#!/usr/bin/env python3
"""Check local links, command coverage and installer synchronization."""
from pathlib import Path
import json
import re
import sys
from urllib.parse import unquote, urlsplit

repo=Path(__file__).resolve().parents[1]
docs=repo/'docs'
files=[p for p in docs.rglob('*.md') if not {'.vuepress','node_modules','l10n'} & set(p.relative_to(docs).parts)]
errors=[]
for page in files:
    text=re.sub(r'```[^\n]*\n.*?```','',page.read_text(),flags=re.S)
    for match in re.finditer(r'!?\[[^\]\n]*\]\(([^\n]+?)\)',text):
        raw=match[1].strip()
        if raw.startswith('<') and raw.endswith('>'):
            raw=raw[1:-1]
        raw=raw.split(' "')[0]
        link=urlsplit(raw)
        if link.scheme or link.netloc or not link.path:
            continue
        target=docs/link.path.lstrip('/') if link.path.startswith('/') else page.parent/link.path
        target=Path(unquote(str(target)))
        if target.suffix=='.html':
            target=target.with_suffix('.md')
        if target.is_dir():
            target=target/'index.md'
        if not target.exists():
            public=docs/'.vuepress/public'/link.path.lstrip('/')
            if not public.exists():
                errors.append(f'{page.relative_to(docs)}: missing {raw}')
        if '.mcc' in link.path:
            errors.append(f'{page.relative_to(docs)}: obsolete Beacon extension {raw}')
coverage=docs/'commands/coverage.json'
if coverage.exists():
    data=json.loads(coverage.read_text())
    entries=data if isinstance(data,list) else data.get('commands',data.get('active_commands',[]))
    if isinstance(entries,dict):
        entries=[{'name':name,'page':page} for name,page in entries.items()]
    names={entry['name'] for entry in entries}
    if len(names) != data.get('activeCommandCount',len(names)):
        errors.append('Command coverage count does not match its manifest')
    required={entry['name'] for entry in data.get('libraryRegistrations',[]) if entry.get('showInIndex')}
    required.update(['clear-console','console-chat','exit','mcc-menu','map','minimap'])
    if required - names:
        errors.append('Undocumented command registrations: ' + ', '.join(sorted(required - names)))
    for command in entries:
        page=command.get('page',command.get('document',command.get('path','')))
        if page and not (docs/page).exists() and not (docs/'commands'/page).exists():
            errors.append(f'Command {command.get("name")}: missing coverage page {page}')
installer=(repo/'tools/install_mcc.py').read_text()
if installer not in (docs/'.vuepress/public/install.sh').read_text():
    errors.append('Public Unix installer differs from canonical tools/install_mcc.py')
if errors:
    print('\n'.join(errors));sys.exit(1)
print(f'Checked {len(files)} documentation pages. Local links and installer synchronization pass.')
