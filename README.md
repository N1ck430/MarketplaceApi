# Setup

- Set `DefaultConnection` ConnectionString in `appsettings.Development.json` (EF for SQL-Server installed)
- Run application (Automatic DB-Migration)
- Test Admin account from `appsettings.Development.json` via swagger

# Optional -> Send Emails

- Disable `SaveMailsToFile` in `appsettings.Development.json`
- Configure SMTP-Settings e.g. with g-mail
