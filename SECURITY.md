# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of GearUp seriously. If you have discovered a security vulnerability, please report it to us as described below.

### How to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please send an email to: shanehtetaung.conceptx.mm@gmail.com

You should receive a response within 48 hours. If for some reason you do not, please follow up via email to ensure we received your original message.

Please include the following information in your report:

- Type of vulnerability (e.g., SQL injection, XSS, authentication bypass)
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### What to Expect

After you submit a report, we will:

1. Confirm receipt of your vulnerability report
2. Investigate and validate the issue
3. Determine the severity and impact
4. Develop and test a fix
5. Release a security update
6. Publicly acknowledge your responsible disclosure (if you wish)

## Security Best Practices

When deploying GearUp, please follow these security best practices:

### Environment Variables

- Never commit `.env` files or sensitive configuration
- Use strong, unique secrets for JWT tokens (minimum 32 characters)
- Rotate secrets regularly
- Use environment-specific configuration files

### Database Security

- Use strong passwords for database users
- Restrict database access to application servers only
- Enable SSL/TLS for database connections in production
- Regularly backup your database
- Keep database server updated with security patches

### API Security

- Always use HTTPS in production
- Enable rate limiting to prevent abuse
- Implement proper authentication and authorization
- Validate and sanitize all user inputs
- Use prepared statements to prevent SQL injection

### Docker Security

- Run containers as non-root user (already configured in our Dockerfile)
- Keep Docker images updated
- Scan images for vulnerabilities regularly
- Use secrets management for sensitive data
- Limit container resources

### Password Security

- Enforce strong password policies
- Use ASP.NET Core Identity's password hasher (already implemented)
- Implement account lockout after failed login attempts
- Never store passwords in plain text

### Dependency Management

- Regularly update NuGet packages
- Review security advisories for dependencies
- Use `dotnet list package --vulnerable` to check for known vulnerabilities
- Enable GitHub Dependabot alerts

### Logging and Monitoring

- Log security-relevant events
- Monitor for suspicious activity
- Implement alerting for security incidents
- Never log sensitive information (passwords, tokens, credit cards)

### CORS Configuration

- Configure CORS to allow only trusted origins
- Avoid using wildcard (*) for allowed origins in production
- Restrict allowed methods and headers

## Known Security Considerations

### Current Implementation

Our application implements several security measures:

- JWT-based authentication with refresh tokens
- Password hashing using ASP.NET Core Identity
- Rate limiting on API endpoints
- FluentValidation for input validation
- Global exception handling (sensitive details hidden in production)
- HTTPS enforcement
- CORS configuration

### Security Features in Development

We are actively working on:

- Two-factor authentication (2FA)
- Account lockout policies
- Security headers (CSP, HSTS, etc.)
- API rate limiting per user
- Audit logging

## Disclosure Policy

When we receive a security bug report, we will:

1. Confirm the problem and determine affected versions
2. Audit code to find similar problems
3. Prepare fixes for all supported versions
4. Release new security fix versions as soon as possible

## Comments on This Policy

If you have suggestions on how this process could be improved, please submit a pull request or open an issue.

---

Thank you for helping keep GearUp and our users safe! 🔒
