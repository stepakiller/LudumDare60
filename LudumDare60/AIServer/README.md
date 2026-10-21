# Сервер ИИ ракет

Игра -> ngrok (HTTPS) -> server.py (порт 8000) -> Ollama (порт 11434) -> модель Qwen.

## 1. Ollama и модель
1. Установи Ollama: https://ollama.com/download
2. Скачай модель (для 8 ГБ видеопамяти):
   ollama pull qwen3:8b
   Если ответы медленные, возьми модель полегче:
   ollama pull qwen3.5:4b
   и поменяй ROCKET_MODEL в start_server.bat.
3. Проверь: ollama run qwen3:8b "hello" (выход: /bye)

## 2. Сервер-прослойка
1. Нужен Python 3.10+ (https://www.python.org, галочка "Add to PATH").
2. В этой папке:
   pip install -r requirements.txt
3. Открой start_server.bat и придумай свой ключ в ROCKET_API_KEY.
   Тот же ключ впиши в RocketAIClient -> Api Key в Unity.
4. Запусти start_server.bat.
5. Проверка в браузере: http://127.0.0.1:8000/health

## 3. ngrok
1. Зарегистрируйся на https://ngrok.com и установи ngrok.
2. ngrok config add-authtoken ТВОЙ_ТОКЕН
3. В панели ngrok (Domains) возьми бесплатный статический домен.
4. ngrok http 8000 --url=https://ТВОЙ-ДОМЕН.ngrok-free.app
   (в старых версиях ngrok вместо --url используется --domain=ТВОЙ-ДОМЕН.ngrok-free.app)
5. Этот адрес впиши в RocketAIClient -> Server Url.

## Порядок запуска
Ollama (запускается сама) -> start_server.bat -> ngrok. Пока всё это работает, ракеты в игре говорят.
Если сервер выключен, игра отвечает запасными фразами.

## Настройки (переменные в start_server.bat)
ROCKET_MODEL            модель Ollama
ROCKET_API_KEY          ключ, который должна присылать игра
ROCKET_RATE_PER_MINUTE  запросов в минуту с одного адреса (20)
ROCKET_PARALLEL         сколько запросов модель обрабатывает одновременно (1)
ROCKET_QUEUE_WAIT       сколько секунд запрос ждёт в очереди (20)
ROCKET_MAX_MESSAGE      максимальная длина сообщения игрока (300)
ROCKET_MAX_HISTORY      сколько прошлых реплик помнит ракета (8)
ROCKET_REVEAL_AFTER     после скольких сообщений игрока ракета точно выдаст код (4)
