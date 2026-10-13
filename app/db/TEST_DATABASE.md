   # Test database

   The integration tests need a real PostgreSQL database, because the database
   rules are part of what is being tested. Without one they are skipped, so a
   green `dotnet test` does not mean they ran.

   ## Setup (once)

   1. Install Docker Desktop and make sure it is running.
   2. In the `app/db` folder, copy `.env.example` to `.env` and fill in your own
      password and a free port. The `.env` file stays on your machine and is
      never committed.
   3. From `app/db`, start the database:

          docker compose up -d

   4. Tell the tests where it is. Use the same password and port as in `.env`:

          setx CIVICCONNECT_TEST_DB "Host=localhost;Port=<PORT>;Database=civicconnect_test;Username=postgres;Password=<PASSWORD>"

      Close and reopen your terminal afterwards, otherwise the variable is not picked up.
   5. From the `app` folder, run the tests:

          dotnet test

      The integration tests should show as passed, not skipped.

   ## Day to day

   - Stop it: `docker compose down` (from `app/db`)
   - Start it again: `docker compose up -d`
   - Nothing needs cleaning by hand. The test fixture deletes and recreates the
     test database at the start of every run.

   ## Things to know

   - Never point CIVICCONNECT_TEST_DB at a shared or real database. The fixture
     wipes whatever it connects to.
   - No Docker? Install PostgreSQL 17, create an empty database called
     `civicconnect_test`, and use its connection string in the variable.
   - If dotnet says "No .NET SDKs were found" although you installed one, run
     `set PATH=C:\Program Files\dotnet;%PATH%` in that terminal.