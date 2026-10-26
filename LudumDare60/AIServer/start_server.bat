@echo off
cd /d %~dp0
set ROCKET_MODEL=qwen3:8b
set ROCKET_API_KEY=eva-code-77
set ROCKET_REVEAL_AFTER=15
set ROCKET_PARALLEL=3
python -m uvicorn server:app --host 127.0.0.1 --port 8000