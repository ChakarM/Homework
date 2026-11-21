# КТ 4. Docker Compose: API + PostgreSQL

Тема проекта: сервис задач
Сущность: task (поля title, done)
Стек: .NET 8, PostgreSQL 16, Docker Compose

## Запуск
docker compose up -d --build
docker compose ps

## Проверка CRUD
curl http://localhost:8080/health
curl -X POST http://localhost:8080/tasks -H "Content-Type: application/json" -d '{"title":"Сдать КТ 4","done":false}'
curl http://localhost:8080/tasks
curl -X PUT http://localhost:8080/tasks/1 -H "Content-Type: application/json" -d '{"title":"Сдать КТ 4","done":true}'
curl -i -X DELETE http://localhost:8080/tasks/2

## Проверка сохранения данных
docker compose down
docker compose up -d
curl http://localhost:8080/tasks

## Скриншоты
screenshots/01-ps.png, 02-crud.png, 03-restart.png, 04-logs.png
