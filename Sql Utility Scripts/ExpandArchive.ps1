
## Move the most recent downloaded .zip file to docker sql container.


#Files Location
$ZipFilesPath = $env:USERPROFILE + "\Downloads"
#Unzip To Same Location
$UnzipPath = "C:\Temp\"
$TempPath = "C:\Temp\Temp"

$Location = $Shell.NameSpace($TempPath)
$ZipFile = Get-Childitem $ZipFilesPath -Filter *.zip | Sort-Object -Descending -Property LastWriteTimeUtc  | select -first 1

#Clear Initialisation Vars from Console
clear

write-host "File: "  $ZipFile.FullName

expand-archive $ZipFile.FullName -DestinationPath $TempPath

$BakFile = Get-ChildItem $TempPath *.bak | Sort-Object -Descending -Property LastWriteTimeUtc  | select -first 1
Move-Item  $BakFile.Fullname "$UnzipPath/sailscores.bak" 


docker cp c:\Temp\sailscores.bak sssql1:/var/opt/mssql/backup/

$dataSource = "localhost"
$database = "master"
$sqlCommand = "RESTORE DATABASE SailScores FROM DISK = '/var/opt/mssql/backup/sailscores.bak' WITH
 MOVE 'DB_84257_sailscores_data' TO '/var/opt/mssql/data/sailscores_data.mdf',
 MOVE 'DB_84257_sailscores_log' TO '/var/opt/mssql/data/sailscores_log.ldf'"

$connectionString = "Data Source=$dataSource; " +
            "Initial Catalog=$database;" +
            "User Id=sa;Password=P@ssw0rd;"

$connection = new-object system.data.SqlClient.SQLConnection($connectionString)
$command = new-object system.data.sqlclient.sqlcommand($sqlCommand,$connection)
$connection.Open()
        
$command.ExecuteNonQuery()

$connection.Close()

Remove-Item –path "$UnzipPath/sailscores.bak"
