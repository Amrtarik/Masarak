localhost:49179
# Masarak Developer Command Reference

This document contains the essential CLI commands required to build, test, and deploy the Masarak platform across different development phases.

---

## 1. Building and Running the Application

### Build the entire solution
Ensure you are in the root directory:
```bash
cd "D:\ITI\GradProj\Masarak"
dotnet build
```

### Run the API server
Run the presentation layer directly:
```bash
cd "D:\ITI\GradProj\Masarak"
dotnet run --project "Masarak.API"
```
*Alternatively, you can navigate directly into `Masarak.API` and run `dotnet run`.*

---

## 2. Entity Framework Core Commands

Since the application uses Clean Architecture, EF Core commands must explicitly point to the `Infrastructure` project (where the DbContext lives) and the `API` project (where the configurations and connection strings live).

### Add a New Migration
Run this when you change the entities in `Masarak.Domain`:
```bash
cd "D:\ITI\GradProj\Masarak"
dotnet ef migrations add PhaseX_MigrationName --project "Masarak.Infrastructure" --startup-project "Masarak.API"
```
*(Replace `PhaseX_MigrationName` with a descriptive name, e.g., `Phase3_AcademicCore`)*

### Update the Database
Run this to apply pending migrations to the SQL Server database:
```bash
cd "D:\ITI\GradProj\Masarak"
dotnet ef database update --project "Masarak.Infrastructure" --startup-project "Masarak.API"
```

### Remove the Last Migration
Run this if you made a mistake and haven't updated the database yet:
```bash
cd "D:\ITI\GradProj\Masarak"
dotnet ef migrations remove --project "Masarak.Infrastructure" --startup-project "Masarak.API"
```

---

## 3. Postman & Newman Automated Testing

Ensure your API is running locally (e.g., on `http://localhost:49179`) before running tests.

### Install Newman CLI (One-time setup)
```bash
npm install -g newman
npm install -g newman-reporter-htmlextra
```

### Run Phase Tests via CLI
Navigate to the Postman folder:
```bash
cd "D:\ITI\GradProj\Masarak\Postman"
```

**Basic Run (Console Output):**
```bash
newman run Masarak_Phase2.postman_collection.json -e Masarak_Local.postman_environment.json -k
```

**Run and Generate HTML Report:**
```bash
newman run Masarak_Phase2.postman_collection.json -e Masarak_Local.postman_environment.json -k -r cli,htmlextra
```
*The HTML report will be saved in a new folder named `newman` inside the Postman directory.*

---

## 4. Phase-Based GitHub Workflow

Follow this Git workflow for each phase to ensure a clean commit history and safe merging.

### Step 4.1: Start a New Phase
When you are ready to start a new phase (e.g., Phase 3), always create a new branch from `main`:
```bash
git checkout main
git pull origin main
git checkout -b phaseN
```

### Step 4.2: Commit Changes Iteratively
As you build the phase, commit often:
```bash
git add .
git commit -m "phaseN Logic"
git push -u origin phaseN
```

### Step 4.3: Merge Phase into Main
Once the phase is complete, tested, and migrations are applied successfully:
```bash
git checkout main
git pull origin main

# Merge the phase branch into main
git merge phase3

# Push the updated main branch to GitHub
git push origin main
```

### Step 4.4: Cleanup
Delete the phase branch locally and remotely (optional, but good practice):
```bash
git branch -d phase3
git push origin --delete phase3
```