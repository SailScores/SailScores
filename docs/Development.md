
# Development of SailScores

## Overview

SailScores is an ASP.NET Core MVC website using an MS-SQL data store. Initially, this document covers development with Visual Studio on a Windows machine. File an issue to request a MacOS version.

## Requirements

### Development environment
[Visual Studio](https://visualstudio.microsoft.com/) Community Edition works well.
[Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver15) Optional, but useful for examination of the database.

### Database
SailScores requires a Microsoft SQL Server database. There are many ways to access a SQL Server database. A Linux Docker container works well to run a local instance.

To run the database in a Docker container:
1. [Docker](https://www.docker.com/get-started) should be installed and running.
2. Download and start a SQL Server container:

        docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=P@ssw0rd' --name 'SailScoreSql' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

## Running the Code

Clone the project from [GitHub](https://github.com/SailScores/SailScores/)
Open Visual Studio, "Open a project or solution" and select the SailScores.sln file.
In the Solution Explorer window in Visual Studio, expand the "SailScores.Web" project and then double click on appsetttings.json to open that file. Make sure the DefaultConnection under ConnectionStrings points at the database server you will use for development. For a Docker container, something like this should work:

    "DefaultConnection": "Server=localhost;Database=sailscores;User Id=sa;password=P@ssw0rd;MultipleActiveResultSets=true"

Right click on the "package.json" file and select "Restore Packages." You may
load a sample database by following the instructions in the next section "Starter Database."

Run the solution by selecting the "Debug" menu and the "Start Debugging" item.
A web browser should open up, and you should see an error:
    
    A database operation failed while processing the request.
    SqlException: Cannot open database "sailscores" requested by the login. The login failed. Login failed for user 'sa'.
    Applying existing migrations for SailScoresContext may resolve this issue
    There are migrations for SailScoresContext that have not been applied to the database
    ...

To use a blank database, click the "Apply Migrations" button to create and update the database. After a pause, the button should change to "Migrations Applied." Refresh the page. You should now get the SailScores home page, but no data is loaded.

### Starter Database

Microsoft provides a [good example](https://docs.microsoft.com/en-us/sql/linux/tutorial-restore-backup-in-sql-server-container?view=sql-server-ver15) of how to restore a database to a SQL docker container. Based on that article, the [starter.bak file](https://github.com/SailScores/SailScores/main/Sql%20Utility%20Scripts/starter.bak) can be used to create a database with test and real club data.

1. If you haven't already, create a container running SQL server:


        docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=P@ssw0rd' --name 'sailscoresDb' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

2. See that the container is running. `docker ps -l` returns the status of the last created container:

         docker ps -l

   returns output similar to

        CONTAINER ID   IMAGE                                        COMMAND                  CREATED         STATUS         PORTS                    NAMES
        f4111464ef6d   mcr.microsoft.com/mssql/server:2022-latest   "/opt/mssql/bin/perm…"   7 minutes ago   Up 7 minutes   0.0.0.0:1433->1433/tcp   sailscoresDb

3. Create a backup directory in the container:

        docker exec -it sailscoresDb mkdir /var/opt/mssql/backup

4. Switch to the sql utility directory in the repository or download another copy of the sample data backup.

        curl -OutFile "starter.bak" "https://github.com/SailScores/SailScores/main/Sql%20Utility%20Scripts/starter.bak"

5. Use docker commands to copy this .bak file to the container directory created above:

        docker cp starter.bak sailscoresDb:/var/opt/mssql/backup

6. [Optional, handy for troubleshooting] Call `sqlcmd` within the container to see the files in the backup. Since we are using the command in the conatiner, this removes the need to install SQL tools on the development machine. (But they're stil useful for other purposes.)

        docker exec -it sailscoresDb /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "P@ssw0rd" -Q "RESTORE FILELISTONLY FROM DISK = '/var/opt/mssql/backup/starter.bak'"

    Buried in the output you'll see a list of files which are used in the next command. The defaults provided below may not need to change.

7. Restore the backup using `sqlcmd` within the container:

        docker exec -it sailscoresDb /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "P@ssw0rd" -Q "RESTORE DATABASE sailscores FROM DISK = '/var/opt/mssql/backup/starter.bak' WITH MOVE 'SailScores_Data' TO '/var/opt/mssql/data/sailscores.mdf', MOVE 'SailScores_Log' TO '/var/opt/mssql/data/sailscores.ldf'"


## Quick Architecture Overview

SailScores is built using ASP.NET Core MVC. It is a single app, with view and core logic thinly separated.

One view of the architecture is by tracing the path of a request for a clubs main score page, such as https://www.sailscores.com/LHYC/ . It will pass through many layers to the database and then back to the browser:
    
  1. Browser requests page

  ... Skipping a few steps, such as localization and determining the club from the initials in the URL...

  2. `ClubController.Index(string clubInitials)` (in SailScores.Web/Controllers/ClubController.cs ) receives request

  3. Controller calls to `SailScores.Web.Services.ClubService.GetClubForClubHome(string clubInitials)` which will build and pass back a Model object. Some calls will skip calling a service in the web project and call directly to a core service (see next step.)

  4. The service in the web project makes various calls to the Core ClubService such as `SailScores.Core.Services.GetMinimalClub(Guid id)`

  5. Core services call into Entity Framework via the `_dbContext` variable which has an instance of `SailScoresContext`.

  6. The calls return from the services to the controller (step 3) which returns an ActionResult, usually built with `View(viewModel)`

  7. ASP.NET uses the files in Views (SailScores.Web/Views/) and its' subdirectories to create HTML that is sent to the browser. These .cshtml files contain a combination of C# and HTML. Logic in these views should focus on the layout of the data contained in the model.

This pattern has the guidelines:

 - Any calls to the database (EF context) should be made from a core service.
 - Core services should encapsulate logic that might be used by different clients, such as a native app. The scoring heavy lifting is done in this layer.
 - Any logic to support web views should be in the web service layer.
