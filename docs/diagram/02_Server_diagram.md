```mermaid
graph LR; Listener-->IocpCore-->Worker; 
Worker-->Session; 
Worker-->DBConnectionPool;
DBConnectionPool-->DBProcedures

Session-->Room; 
Room-->JobQueue; 
```