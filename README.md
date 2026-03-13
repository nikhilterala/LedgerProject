# Ledger System V1

A simple but robust double-entry ledger system built to understand how real financial systems maintain transaction integrity.

This project was created as a learning exercise to explore how production financial systems handle:

- immutable transaction records
- balance reconciliation
- transaction safety
- operational controls

The goal was not just to build CRUD APIs, but to model the core rules financial systems follow to protect money movement.


------------------------------------------------------------
PROJECT OVERVIEW
------------------------------------------------------------

This system provides a backend and admin console for managing a financial ledger.

Key capabilities include:

- Creating multi-entry transactions
- Maintaining an immutable ledger
- Ensuring transactions are always zero-sum
- Supporting transaction reversals
- Supporting adjustment transactions
- Detecting mismatches between ledger and balances
- Freezing accounts when inconsistencies are detected
- Admin workflows to investigate and unfreeze accounts
- Audit logging for important operations

A small React admin interface is included to operate the system.


------------------------------------------------------------
CORE CONCEPTS IMPLEMENTED
------------------------------------------------------------

1. Immutable Ledger

Ledger entries are append-only.

Once written:
- they cannot be edited
- they cannot be deleted

If something needs to be corrected, a reversal or adjustment transaction must be created.


2. Double Entry Accounting

Every transaction must sum to zero.

Example:

Account A   -100
Account B   +100

This guarantees that money never disappears from the system.


3. Cached Account Balances

Balances are stored in a separate table for fast reads.

However:

Ledger = source of truth  
Balance table = performance optimization

A reconciliation job verifies balances against ledger entries.


4. Reconciliation Engine

The reconciliation process compares:

Ledger calculated balance
vs
Cached account balance

If a mismatch is detected:

- the account is automatically frozen
- an administrator must investigate


5. Transaction Idempotency

Each transaction request contains an Idempotency Key.

This prevents duplicate transactions when requests are retried due to network failures.


6. Role Based Security

The system supports three roles:

Admin
- create users
- run reconciliation
- unfreeze accounts
- perform adjustments

Operator
- create transactions
- view accounts
- view transactions

User
- view statements



------------------------------------------------------------
TECH STACK
------------------------------------------------------------

Backend

- .NET 9 Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- BCrypt password hashing


Frontend

- React
- Material UI
- Axios
- React Router



------------------------------------------------------------
PROJECT STRUCTURE
------------------------------------------------------------

Backend

LedgerProject
│
├── Controllers
├── Services
├── Models
├── Data
├── Requests
└── Program.cs


Frontend

ledger-ui
│
├── src
│   ├── api
│   ├── components
│   ├── pages
│   └── App.js



------------------------------------------------------------
MAIN FEATURES
------------------------------------------------------------

Transactions
- Create multi-entry transactions
- Zero-sum validation
- Transaction idempotency

Ledger
- Immutable ledger entries
- Full transaction history

Corrections
- Reversal transactions
- Adjustment transactions

Integrity Controls
- Balance reconciliation
- Automatic account freeze
- Admin unfreeze workflow

Security
- JWT authentication
- Role-based authorization

Admin Console
- Dashboard
- Account list
- Transaction monitoring
- Statement viewer
- User creation
- Reconciliation trigger



------------------------------------------------------------
RUNNING THE PROJECT
------------------------------------------------------------

Backend

Run the API project:

dotnet run

Make sure SQL Server is running and connection strings are configured.


Frontend

From the React project directory:

npm install
npm start

The UI will start on:

http://localhost:3000



------------------------------------------------------------
WHY THIS PROJECT EXISTS
------------------------------------------------------------

Most tutorials teach financial systems as simple CRUD applications.

In reality, financial platforms must protect against:

- duplicate requests
- data corruption
- reconciliation mismatches
- operational errors

This project explores those real-world safety mechanisms in a simplified environment.



------------------------------------------------------------
CURRENT STATUS
------------------------------------------------------------

Version 1 — Complete

The system currently supports:

- transaction processing
- reconciliation
- account freezing
- admin operations
- operator dashboard


Future improvements could include:

- background reconciliation jobs
- reporting APIs
- improved UI
- event sourcing
- audit dashboards



------------------------------------------------------------
LEARNING OUTCOMES
------------------------------------------------------------

Building this project helped explore:

- ledger architecture
- financial data integrity
- idempotent APIs
- reconciliation patterns
- operational controls
- backend security
- admin tooling



------------------------------------------------------------
LICENSE
------------------------------------------------------------

This project is intended for learning and experimentation.

Feel free to fork and build on it.
