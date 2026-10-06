# SQL Server for Railway. start.sh adapts it to Railway volumes and limits;
# see the comments there.
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root
COPY start.sh /usr/local/bin/start-mssql.sh
RUN sed -i 's/\r$//' /usr/local/bin/start-mssql.sh && chmod +x /usr/local/bin/start-mssql.sh
CMD ["/usr/local/bin/start-mssql.sh"]
