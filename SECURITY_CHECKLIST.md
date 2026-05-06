# UniDesk security checklist

- Server-side validation is shared by MVC and API request models (`Required`, `StringLength`).
- MVC forms must include an anti-CSRF token and unsafe MVC actions must validate it.
- API endpoints must reject invalid input with `400 Bad Request` and validation problem details.
- Responses include `X-Content-Type-Options: nosniff`.
- Responses include `X-Frame-Options: DENY`.
- Form risk: a ticket creation form without a valid anti-CSRF token could allow an unwanted cross-site form submission from another page.

