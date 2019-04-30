
Select top 100
Message,
--Level,
TimeStamp,
Properties.value('(/properties/property[@key="ElapsedMilliseconds"])[1]', 'varchar(50)'),
--Properties.value('(/properties/property[@key="SourceContext"])[1]', 'nvarchar(max)'),
Properties.value('(/properties/property[@key="StatusCode"])[1]', 'nvarchar(20)'),
Properties.value('(/properties/property[@key="RequestPath"])[1]', 'nvarchar(80)')
,
Properties
 from Logs
 WHERE
 
Properties.value('(/properties/property[@key="StatusCode"])[1]', 'nvarchar(20)') <> '307'
-- WHERE
--
--Properties.value('(/properties/property[@key="SourceContext"])[1]', 'varchar(100)') in
--('Microsoft.AspNetCore.Hosting.Internal.WebHost')
order by Id  desc


Select DATEADD(day, DATEDIFF(day, 0, TimeStamp), 0), count(*) from Logs
group by DATEADD(day, DATEDIFF(day, 0, TimeStamp), 0)
order by DATEADD(day, DATEDIFF(day, 0, TimeStamp), 0)

