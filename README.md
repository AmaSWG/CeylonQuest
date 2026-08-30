# CeylonQuest QA Testing

This folder contains the QA testing resources and automated tests for the CeylonQuest project.

The purpose of this test project is to verify the functionality of the system during development
and identify defects before features are considered complete.

## Project Structure

- `Tests/` - Contains automated test cases.
- `Pages/` - Contains Page Object Model classes used by the automated tests.
- `TestData/` - Contains test data used during test execution.
- `Utilities/` - Contains reusable helper functions and utilities.
- `Configuration/` - Contains configuration-related classes and files.

## Test Environment

The tests are currently executed against the local development environment.

Local environment-specific configuration should not be committed to the repository.

Files such as the following should remain local:

- `appsettings.Local.json`
- `local.runsettings`
- `bin/`
- `obj/`
- `TestResults/`

## Running the Tests

Navigate to the QA test project directory:

```bash
cd QA

# Restore the required dependencies:

dotnet restore

# Run the test:

dotnet test
