# API Gateway

Gateway: http://localhost:5000
Identity Service: http://localhost:5278

Routes:

- POST /api/auth/register → Identity Service

How to test:

1. Run the Identity Service locally (must be running on http://localhost:5278).
2. Run the API Gateway project (it listens on http://localhost:5000).
3. Send a POST request to http://localhost:5000/api/auth/register with the same JSON body you would send to the Identity Service — the gateway will proxy the request to the Identity Service and return the response unchanged.

Example curl:

curl -X POST http://localhost:5000/api/auth/register \
	-H "Content-Type: application/json" \
	-d '{"firstName":"Test","lastName":"Visitor","email":"test.verify@example.com","phoneNumber":"0771234567","nationality":"Sri Lankan","password":"Test@12345","confirmPassword":"Test@12345","registrationType":"Visitor"}'
