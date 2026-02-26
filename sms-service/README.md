# SMS Service

SMS service with authentication via Aegis Auth Server.

## Endpoints

### Auth
- `POST /api/auth/login` - customer login
- `POST /api/auth/login/user` - user login
- `GET /api/auth/health` - health check

### Azure SMS (from: +18332702587)
- `POST /azure/sms/send` - send SMS
- `POST /azure/sms/send-with-link` - send SMS with auto-shortened link
- `POST /azure/sms/send-bulk` - bulk SMS
- `POST /azure/sms/diagnostic` - diagnostic test

### Twilio SMS (from: configured in Twilio:FromPhoneNumber)
- `POST /twilio/sms/send` - send SMS
- `POST /twilio/sms/send-with-link` - send SMS with auto-shortened link
- `POST /twilio/sms/send-bulk` - bulk SMS

### Other
- `GET /health` - service status
