const html = document.documentElement;
const statusEl = document.getElementById("status");
const sendBtn = document.getElementById("send");
const stopBtn = document.getElementById("stop");
const clearBtn = document.getElementById("clear");
const clearChatBtn = document.getElementById("clear-chat-btn");
const clearToolsBtn = document.getElementById("clear-tools-btn");
const promptEl = document.getElementById("prompt");
const chatEl = document.getElementById("chat");
const toolsEl = document.getElementById("tools");
const emptyStateEl = document.getElementById("empty-state");
const toolsEmptyStateEl = document.getElementById("tools-empty-state");
const typingIndicatorEl = document.getElementById("typing-indicator");
const themeToggleBtn = document.getElementById("theme-toggle");
const themeToggleIconEl = document.getElementById("theme-toggle-icon");

let history = [];
let activeAssistantBody = null;
let abortController = null;

stopBtn.disabled = true;

loadTheme();
loadConfig();

themeToggleBtn.addEventListener("click", () => {
  const next = html.getAttribute("data-theme") === "dark" ? "light" : "dark";
  setTheme(next);
});

sendBtn.addEventListener("click", sendPrompt);
stopBtn.addEventListener("click", () => abortController?.abort());

clearBtn.addEventListener("click", () => {
  history = [];
  removeAllMessages();
  removeAllTimelineEvents();
  promptEl.value = "";
  activeAssistantBody = null;
  updateEmptyStates();
});

clearChatBtn.addEventListener("click", () => {
  history = [];
  removeAllMessages();
  promptEl.value = "";
  activeAssistantBody = null;
  updateEmptyStates();
});

clearToolsBtn.addEventListener("click", () => {
  removeAllTimelineEvents();
  updateEmptyStates();
});

promptEl.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && !event.shiftKey) {
    event.preventDefault();
    sendPrompt();
  }
});

async function loadConfig() {
  try {
    const response = await fetch("/api/config");
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const config = await response.json();
    const modelLabel = config.model ? config.model : "Model not configured";
    statusEl.textContent = config.hasApiKey ? modelLabel : `${modelLabel} / missing OPENROUTER_API_KEY`;
  } catch (error) {
    statusEl.textContent = `Config error: ${error.message}`;
  }
}

async function sendPrompt() {
  const prompt = promptEl.value.trim();
  if (!prompt || abortController) {
    return;
  }

  history.push({ role: "user", content: prompt });
  addMessage("user", prompt);
  promptEl.value = "";
  activeAssistantBody = addMessage("assistant", "");
  setBusy(true);

  abortController = new AbortController();

  try {
    const response = await fetch("/api/chat/stream", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ messages: history }),
      signal: abortController.signal
    });

    if (!response.ok || !response.body) {
      throw new Error(`HTTP ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    let finalAssistantText = "";

    while (true) {
      const { value, done } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      buffer = parseSseChunk(buffer, (eventName, envelope) => {
        addTimelineEvent(eventName, envelope);

        if (eventName === "error") {
          const errorMessage = envelope.data?.message ?? "Unknown error";
          addMessage("error", errorMessage);
        }

        if (eventName === "final") {
          finalAssistantText = formatFinalText(envelope.data);
          activeAssistantBody.textContent = finalAssistantText;
        }

        if (eventName === "state_summary") {
          const turnCount = envelope.data?.turnCount ?? "?";
          const toolCallCount = envelope.data?.toolCallCount ?? "?";
          statusEl.textContent = `Running turn ${turnCount}, tools ${toolCallCount}`;
        }
      });
    }

    if (finalAssistantText.trim().length > 0) {
      history.push({ role: "assistant", content: finalAssistantText });
    }
  } catch (error) {
    if (error.name !== "AbortError") {
      addMessage("error", `Request failed: ${error.message}`);
      addTimelineEvent("error", {
        kind: "error",
        data: {
          code: "request_failed",
          message: error.message
        }
      });
    }
  } finally {
    abortController = null;
    activeAssistantBody = null;
    setBusy(false);
  }
}

function parseSseChunk(buffer, onEvent) {
  let blockIndex;
  while ((blockIndex = buffer.indexOf("\n\n")) >= 0) {
    const rawBlock = buffer.slice(0, blockIndex);
    buffer = buffer.slice(blockIndex + 2);

    let eventName = "message";
    let dataText = "";
    for (const line of rawBlock.split("\n")) {
      if (line.startsWith("event:")) {
        eventName = line.slice(6).trim();
      } else if (line.startsWith("data:")) {
        dataText += line.slice(5).trim();
      }
    }

    if (!dataText) {
      continue;
    }

    try {
      onEvent(eventName, JSON.parse(dataText));
    } catch (error) {
      onEvent("error", {
        kind: "error",
        data: {
          code: "invalid_sse_payload",
          message: "Failed to parse SSE payload.",
          detail: dataText
        }
      });
    }
  }

  return buffer;
}

function addMessage(role, content) {
  const wrapper = document.createElement("div");
  wrapper.className = `message ${role}`;

  const label = document.createElement("div");
  label.className = "message-label";
  label.textContent = role;

  const body = document.createElement("div");
  body.className = "message-body";
  body.textContent = content;

  wrapper.append(label, body);
  chatEl.insertBefore(wrapper, typingIndicatorEl);
  chatEl.scrollTop = chatEl.scrollHeight;
  updateEmptyStates();
  return body;
}

function addTimelineEvent(kind, envelope) {
  const event = document.createElement("div");
  event.className = `timeline-event kind-${kind}`;

  const label = document.createElement("div");
  label.className = "timeline-label";
  label.textContent = kind.replaceAll("_", " ");

  const body = document.createElement("div");
  body.className = "timeline-body-text";
  body.textContent = JSON.stringify(envelope.data ?? envelope, null, 2);

  event.append(label, body);
  toolsEl.appendChild(event);
  toolsEl.scrollTop = toolsEl.scrollHeight;
  updateEmptyStates();
}

function formatFinalText(data) {
  if (!data) {
    return "The run completed without a final payload.";
  }

  const lines = [];
  if (data.headline) {
    lines.push(data.headline);
    lines.push("");
  }

  if (data.answerMarkdown) {
    lines.push(data.answerMarkdown);
  }

  if (Array.isArray(data.verifiedFacts) && data.verifiedFacts.length > 0) {
    lines.push("");
    lines.push("Verified facts:");
    for (const fact of data.verifiedFacts) {
      lines.push(`- ${fact}`);
    }
  }

  if (Array.isArray(data.openIssues) && data.openIssues.length > 0) {
    lines.push("");
    lines.push("Open issues:");
    for (const issue of data.openIssues) {
      lines.push(`- ${issue}`);
    }
  }

  if (data.nextAction) {
    lines.push("");
    lines.push(`Next action: ${data.nextAction}`);
  }

  return lines.join("\n");
}

function setBusy(busy) {
  sendBtn.disabled = busy;
  stopBtn.disabled = !busy;
  promptEl.disabled = busy;
  typingIndicatorEl.classList.toggle("visible", busy);
  statusEl.classList.toggle("busy", busy);
  if (!busy) {
    loadConfig();
  } else {
    statusEl.textContent = "Streaming run...";
  }
}

function removeAllMessages() {
  for (const message of chatEl.querySelectorAll(".message")) {
    message.remove();
  }
}

function removeAllTimelineEvents() {
  for (const event of toolsEl.querySelectorAll(".timeline-event")) {
    event.remove();
  }
}

function updateEmptyStates() {
  emptyStateEl.style.display = chatEl.querySelectorAll(".message").length === 0 ? "" : "none";
  toolsEmptyStateEl.style.display = toolsEl.querySelectorAll(".timeline-event").length === 0 ? "" : "none";
}

function loadTheme() {
  const theme = localStorage.getItem("mcc-playground-theme") || "dark";
  setTheme(theme);
}

function setTheme(theme) {
  html.setAttribute("data-theme", theme);
  themeToggleIconEl.textContent = theme === "dark" ? "◎" : "◐";
  localStorage.setItem("mcc-playground-theme", theme);
}
