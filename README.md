//Развернуть бэкап БД 
psql.exe -U postgres -d test  -f store.sql
//Настроить конфиг
app.config.json