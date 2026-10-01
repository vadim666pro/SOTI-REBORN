# Свои роли и департаменты

Короткая инструкция для добавления профессии (job) и объединения нескольких профессий в департамент. В примерах `ExampleRole` и `ExampleDepartment` замените на свои ID. ID чувствительны к регистру и должны совпадать во всех файлах.

## 1. Добавьте playtime tracker

Для каждой новой профессии создайте tracker в `Resources/Prototypes/Roles/play_time_trackers.yml`:

```yml
- type: playTimeTracker
  id: JobExampleRole
```

Значение `id` затем указывается в поле `playTimeTracker` профессии.

## 2. Опишите профессию

Создайте файл, например `Resources/Prototypes/Roles/Jobs/Example/example_role.yml`:

```yml
- type: job
  id: ExampleRole
  name: job-name-example-role
  description: job-description-example-role
  playTimeTracker: JobExampleRole
  startingGear: ExampleRoleGear
  icon: JobIconPassenger
  supervisors: job-supervisors-everyone
  canBeAntag: false

- type: startingGear
  id: ExampleRoleGear
  equipment:
    ears: ClothingHeadsetGrey
    jumpsuit: ClothingUniformJumpsuitColorGrey
```

Основные поля:

- `id` — внутренний ID профессии. Используйте его в department, spawnpoint и gamerule.
- `name`, `description` — ключи локализации, а не отображаемый текст.
- `playTimeTracker` — ID tracker из предыдущего шага.
- `startingGear` — ID набора экипировки. Можно использовать существующий набор или описать свой ниже в том же YAML.
- `icon` — ID существующей иконки job, например `JobIconPassenger`.
- `supervisors` — ключ локализации для строки «кому подчиняется».
- `canBeAntag` — разрешать ли выбирать эту профессию антагонистам.

`access`, `accessGroups`, `requirements` и `special` добавляйте, только если они нужны. Названия предметов экипировки должны быть существующими prototype ID.

## 3. Добавьте локализацию

В `Resources/Locale/en-US/job/job-names.ftl` добавьте имя:

```ftl
job-name-example-role = Example Role
```

В `Resources/Locale/en-US/job/job-description.ftl` добавьте описание:

```ftl
job-description-example-role = A short description of the role.
```

Для русской локали добавьте те же ключи в `Resources/Locale/ru-RU/job/job-names.ftl` и `Resources/Locale/ru-RU/job/job-description.ftl`:

```ftl
job-name-example-role = Пример профессии
job-description-example-role = Краткое описание профессии.
```

Ключи после `name` и `description` в job prototype должны в точности совпадать с ключами FTL.

## 4. Создайте департамент

Добавьте запись в `Resources/Prototypes/Roles/Jobs/departments.yml`:

```yml
- type: department
  id: ExampleDepartment
  name: department-ExampleDepartment
  description: department-ExampleDepartment-description
  color: "#4477AA"
  roles:
  - ExampleRole
```

Если в департаменте несколько профессий, перечислите все их job ID в `roles`. В `Resources/Locale/en-US/job/department.ftl` добавьте:

```ftl
department-ExampleDepartment = Example Department
```

А в `Resources/Locale/en-US/job/department-desc.ftl`:

```ftl
department-ExampleDepartment-description = A short description of this department.
```

Переводы для русскоязычного клиента добавьте в `Resources/Locale/ru-RU/job/department.ftl` и `Resources/Locale/ru-RU/job/department-desc.ftl` с теми же ключами.

## 5. Добавьте точку спавна

Профессия не получит собственное место появления автоматически. Создайте prototype в `Resources/Prototypes/Entities/Markers/Spawners/jobs.yml` или отдельном YAML-файле в этой папке:

```yml
- type: entity
  id: SpawnPointExampleRole
  parent: SpawnPointJobBase
  name: example role spawn point
  components:
  - type: SpawnPoint
    job_id: ExampleRole
  - type: Sprite
    layers:
    - state: green
    - state: passenger
```

Разместите `SpawnPointExampleRole` на карте в виде entity с компонентом `Transform` и координатами. Если точка отсутствует, система спавна может выбрать другое доступное место или общий fallback.

Для специального режима можно создать job-specific spawnpoint отдельным prototype и размещать его только на нужной карте.

## 6. Подключите роль к режиму

Добавление job и department само по себе не запускает режим и не назначает игрокам команды.

- Для обычной профессии проверьте, что профессия доступна в `StationJobs` нужной станции и у неё есть точка спавна.
- Для gamerule добавьте его prototype в map `MapAutoGameRule.rules` или запускайте через preset. При необходимости настройте `minPlayers` в компоненте `GameRule`.
- Если gamerule определяет команды по job ID, добавьте новый ID в соответствующий список/проверку системы режима и протестируйте подсчёт живых игроков, выбор победителя и респавн.
- Если профессия должна быть доступна только в особом режиме, не добавляйте её в обычную ротацию станции; назначайте её из gamerule и настройте отдельный spawnpoint.

В RIOT принадлежность к команде определяется в `Content.Server/CounterStrike/Systems/RiotRoundControllerSystem.cs` по job ID. Добавляя туда роль, обновите team lookup и логику назначения/спавна, а не только список department.

## Проверка

Перед запуском проверьте, что job ID, tracker ID, department `roles`, spawnpoint `job_id` и ключи локализации совпадают. Сборка поможет найти ошибки C# и prototype references. В этом checkout YAML linter может остановиться раньше проверки файлов из-за известной ошибки отсутствующего состояния `mag-0` в `awp.rsi`.
