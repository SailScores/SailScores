
Select top 20
Message,
Level,
TimeStamp,
Properties.value('(/properties/property[@key="ElapsedMilliseconds"])[1]', 'varchar(50)'),
Properties.value('(/properties/property[@key="SourceContext"])[1]', 'nvarchar(max)'),
Properties.value('(/properties/property[@key="Status"])[1]', 'nvarchar(20)')
,
Properties
 from Logs
-- WHERE
--
--Properties.value('(/properties/property[@key="SourceContext"])[1]', 'varchar(100)') in
--('Microsoft.AspNetCore.Hosting.Internal.WebHost')
order by Id  desc


