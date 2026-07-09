
# LeatherLane Atelier

A full-stack e-commerce application for a leather products store with authentication and transaction management.

## Features

- User registration and login
- JWT-based authentication
- Transaction history
- Checkout process
- Beautiful, responsive design

## Tech Stack

- Backend: Node.js, Express, MongoDB, Mongoose, JWT, bcrypt
- Frontend: React, Vite, React Router, Axios

## Setup

### Prerequisites

- Node.js
- MongoDB (running locally or use MongoDB Atlas)

### Installation

1. Clone the repo
2. Backend setup:
   ```bash
   cd backend
   npm install
   ```
   Update `.env` with your MongoDB URI and JWT secret

3. Frontend setup:
   ```bash
   cd frontend
   npm install
   ```

## Running the App

1. Start MongoDB
2. Start backend:
   ```bash
   cd backend
   npm run dev
   ```
3. Start frontend:
   ```bash
   cd frontend
   npm run dev
   ```

Backend runs on http://localhost:5000, frontend on http://localhost:3000

