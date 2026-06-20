# Shagun – Wedding Gift Contribution Platform

Shagun is a modern, full-stack application built for hosts (bride, groom, family) to publish their wedding gift registry, and for invitees to contribute cash towards those items.

## Technologies Used

*   **Backend:** Python 3.12, FastAPI, SQLAlchemy ORM, Uvicorn
*   **Database:** MySQL
*   **Payments:** Razorpay API (with development mock fallback)
*   **Frontend:** React 19, Vite, Material UI (MUI), Redux Toolkit, Axios

---

## Features

1.  **Host Dashboard:**
    *   Host Registration and JWT login.
    *   Wedding Details setup (names, venue, date, description, cover photo).
    *   Gift Catalog management (add custom registry items, target prices, upload images).
    *   Invitee Management (add manually or upload guest lists via CSV).
    *   Track contribution metrics (total targets, amount received, percentage bars).
    *   Export contributions details report as CSV.

2.  **Invitee Page:**
    *   Open customized tokenized link (e.g. `/invite/{token}`).
    *   View wedding info and registry catalog.
    *   Contribute custom or pre-filled cash amounts towards selected gifts.
    *   Contribute anonymously option.
    *   Embedded Razorpay checkout modal.

3.  **Admin Console:**
    *   Admin dashboard to manage registered hosts, check wedding events, and review database logs/payments.

---

## Setup Instructions

### Prerequisites
*   Python 3.12+
*   Node.js 18+
*   MySQL Service running locally or on a server

### 1. Database Creation
Create a MySQL database named `shagun`.
```sql
CREATE DATABASE IF NOT EXISTS shagun;
```

### 2. Backend Setup
1. Navigate to the backend directory:
    ```bash
    cd backend
    ```
2. Create and activate a Python virtual environment:
    ```bash
    python3 -m venv venv
    source venv/bin/activate
    ```
3. Install dependencies:
    ```bash
    pip install -r requirements.txt
    ```
4. Configure `.env` using `.env.example` as a template. Make sure to specify the correct MySQL connection string:
    ```env
    DATABASE_URL=mysql://nagesh:test123@localhost/shagun_dbs
    ```
5. Start the FastAPI development server:
    ```bash
    uvicorn app.main:app --reload --port 8000
    ```

### 3. Frontend Setup
1. Navigate to the frontend directory:
    ```bash
    cd ../frontend
    ```
2. Install npm packages:
    ```bash
    npm install
    ```
3. Start the Vite React development server:
    ```bash
    npm run dev
    ```
4. Access the application in your browser at `http://localhost:5173`.
