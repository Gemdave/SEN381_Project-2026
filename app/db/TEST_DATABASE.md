\# Test database



The integration tests, such as `ConcurrentAcceptTests`, need a real PostgreSQL database because they test database behaviour. Without one, these tests may be skipped, so a green `dotnet test` result does not necessarily mean they ran.



\## Setup (once)



1\. Install Docker Desktop and make sure it is running.



2\. From the `app/db` folder, start the database:



&#x20;  ```cmd

&#x20;  docker compose up -d

&#x20;  ```



3\. Tell the tests where the database is located. In Windows CMD, run:



&#x20;  ```cmd

&#x20;  setx CIVICCONNECT\_TEST\_DB "Host=localhost;Port=5433;Database=civicconnect\_test;Username=postgres;Password=test"

&#x20;  ```



&#x20;  Close and reopen your terminal or VS Code afterwards so the new environment variable is available.



4\. From the `app` folder, run the tests:



&#x20;  ```cmd

&#x20;  dotnet test

&#x20;  ```



&#x20;  Check the output to confirm that the integration tests passed rather than being skipped.



\## Day to day



\- Stop the database by running `docker compose down` from the `app/db` folder.

\- Start it again by running `docker compose up -d` from the same folder.

\- The test fixture is expected to delete and recreate the test database at the start of each run, so manual cleanup should not be necessary.



\## Things to know



\- \*\*Never\*\* point `CIVICCONNECT\_TEST\_DB` at the Neon database. The test fixture may delete data in whichever database it connects to.

\- If you do not use Docker, you can install PostgreSQL 17, create an empty database named `civicconnect\_test`, and use its connection string in the environment variable instead.

\- The optional `db/seed/seed\_requests.sql` file adds 1,000 dummy requests for performance testing. It is not required for the integration tests.

\- CI will use its own test database. Gerald is setting that up.

