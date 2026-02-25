# Миграции.

## ВАЖНО! Смена контекста (appsettings) для которых будут выполняться все (!) команды.

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:ASPNETCORE_ENVIRONMENT='Development'
```

```bash
cd server/Ac.Api
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_ENVIRONMENT=Development
```

## Создание миграций на базе измененых моделей.

```powershell
Add-Migration AddEventsNotifyTrigger -o Migrations/Api -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
```

```bash
cd server/Ac.Api && dotnet ef migrations add RemoveTenantTables -o Migrations/Api --project ../Ac.Data --context ApiDb
```

## Применение всех изменений к DB.

```powershell
Update-Database -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
```

```bash
cd server/Ac.Api && dotnet ef database update --project ../Ac.Data --context ApiDb
```

## Миграция в несколько шагов.

```powershell
Add-Migration "AddProfile_Step1" -o Migrations/Api -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
Add-Migration "AddProfile_Step2" -o Migrations/Api -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
Add-Migration "AddProfile_Step3" -o Migrations/Api -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
```

```bash
cd server/Ac.Api
dotnet ef migrations add AddProfile_Step1 -o Migrations/Api --project ../Ac.Data --context ApiDb
dotnet ef migrations add AddProfile_Step2 -o Migrations/Api --project ../Ac.Data --context ApiDb
dotnet ef migrations add AddProfile_Step3 -o Migrations/Api --project ../Ac.Data --context ApiDb
```

# Откат миграций и/или замена текущей миграции.

## Простой откат еще не примененной (без update-database) миграции. Например, видим что криво сгенерилось или что-то забыли.

```powershell
Remove-Migration -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
```

```bash
cd server/Ac.Api && dotnet ef migrations remove --project ../Ac.Data --context ApiDb
```

## Откат до нужной версии - выбраем последнюю что нужно сохранить.

```powershell
# 1)
Update-Database -Migration ThumbForImages -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
# 2)
Remove-Migration -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
# 3) Если надо применить новую миграцию
Add-Migration "AddNewPaymentVersion" -o Migrations/Api -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
# 4)
Update-Database -Project Ac.Data -StartupProject Ac.Api -Context ApiDb
```

```bash
cd server/Ac.Api
dotnet ef database update ThumbForImages --project ../Ac.Data --context ApiDb
dotnet ef migrations remove --project ../Ac.Data --context ApiDb
dotnet ef migrations add AddNewPaymentVersion -o Migrations/Api --project ../Ac.Data --context ApiDb
dotnet ef database update --project ../Ac.Data --context ApiDb
```
