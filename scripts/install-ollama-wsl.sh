#!/usr/bin/env bash
set -euo pipefail

MODEL_TAG="${MODEL_TAG:-qwen2.5-coder:7b-instruct-q5_K_M}"
MODEL_VERSION="${MODEL_VERSION:-qwen2.5-coder-7b-instruct-q5_K_M}"
MODELS_ROOT="${MODELS_ROOT:-/mnt/c/writable/models}"
SKIP_MODEL_PULL="${SKIP_MODEL_PULL:-false}"

if ! command -v ollama >/dev/null 2>&1; then
  echo "Installing Ollama..."
  curl -fsSL https://ollama.com/install.sh | sh
else
  echo "Ollama is already installed."
  ollama --version
fi

MODEL_DIR="${MODELS_ROOT}/${MODEL_VERSION}"
mkdir -p "${MODEL_DIR}"

if [ "${SKIP_MODEL_PULL}" != "true" ]; then
  echo "Using model cache: ${MODEL_DIR}"
  export OLLAMA_MODELS="${MODEL_DIR}"
  echo "Pulling model: ${MODEL_TAG}"
  ollama pull "${MODEL_TAG}"
fi
