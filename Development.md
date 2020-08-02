
# Development of SailScores

## Overview

SailScores is an ASP.NET Core MVC website using an MS-SQL data store. Initially, this document covers development on a Windows PC. MacOS instructions should get added soon.

## Requirements

### Development environment
[Visual Studio](https://visualstudio.microsoft.com/) Community Edition works well.

### Database
SailScores requires a Microsoft SQL Server database. There are many ways to access a SQL Server database. A Linux Docker container works well to run a local instance.

To run the database in a Docker container:
1. [Docker](https://www.docker.com/get-started) should be installed and running.
2. Download and start a SQL Server container:

        docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=P@ssw0rd' --name 'SailScoreSql' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest

### Code

Clone the project from [GitHub](https://github.com/jbfraser1/SailScores)
Open Visual Studio, "Open a project or solution" and select the SailScores.sln file.
In the Solution Explorer window in Visual Studio, expand the "SailScores.Web" project and then double click on appsetttings.json to open that file. Make sure the DefaultConnection under ConnectionStrings has a connection that points at the database server you will use for development. For a Docker container, something like this should work:

    "DefaultConnection": "Server=localhost;Database=sailscores;User Id=sa;password=P@ssw0rd;MultipleActiveResultSets=true"

Right click on the "package.json" file and select "Restore Packages"
Run the solution by selecting the "Debug" menu and the "Start Debugging" item.
A web browser should open up, and you should see an error:
    
    A database operation failed while processing the request.
    SqlException: Cannot open database "sailscores" requested by the login. The login failed. Login failed for user 'sa'.
    Applying existing migrations for SailScoresContext may resolve this issue
    There are migrations for SailScoresContext that have not been applied to the database
    ...

Click the "Apply Migrations" button to create and update the database. After a pause, the button should change to "Migrations Applied." Refresh the page. You should now get the SailScores home page, but no data is loaded.


### Still need to add

- Seed data.
- Architecture overview
- Commonly used tech: EF, AutoMapper, Typescript

