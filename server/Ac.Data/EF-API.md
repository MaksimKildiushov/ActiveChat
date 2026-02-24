# Первая миграция.
Add-Migration "Initial" -o Migrations -Project Ac.Data -StartupProject Ac.Api -c ApiDb

# Создание миграций на базе измененых моделей.
Add-Migration "RemoveTenantTables" -o Migrations -Project Ac.Data -StartupProject Ac.Api -c ApiDb


# Применение всех изменений к DB.
update-database -Project Ac.Data -StartupProject Ac.Api -Context ApiDb

# Миграция в несколько шагов.
Add-Migration "AddProfile_Step1" -o Migrations
Add-Migration "AddProfile_Step2" -o Migrations
Add-Migration "AddProfile_Step3" -o Migrations
Add-Migration "AddProfile_Step4" -o Migrations

## Откат миграций и/или замена текущей миграции.

# Простой откат еще не примененной (без update-database) миграции. Например, видим что криво сгенерилось или что-то забыли.
Remove-Migration -Project Ac.Data -StartupProject Ac.Api

# Откат до нужной версии - выбраем последнюю что нужно сохранить.
1) update-database -migration ThumbForImages
# Очищаем все миграции до.
2) Remove-Migration -Project Ac.Data -StartupProject Ac.Api
# *Если надо применить новую миграцию (если нет, то делаем code reset).
3) Add-Migration "AddNewPaymentVersion" -o Migrations
4) update-database -Project Ac.Data -StartupProject Ac.Api




## Смена контекста (appsettings) для которых будут выполняться все (!) команды PMC.
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:ASPNETCORE_ENVIRONMENT='Development'
Update-Database
$env:ASPNETCORE_ENVIRONMENT='aaaaadaaaaa'
