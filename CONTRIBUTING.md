# Contributing to GearUp

Thank you for your interest in contributing to GearUp! This document provides guidelines and instructions for contributing to the project.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)

## 🤝 Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## 🚀 Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/GearUp.git
   cd GearUp
   ```
3. **Add the upstream repository**:
   ```bash
   git remote add upstream https://github.com/Rahull-Adk/GearUp.git
   ```
4. **Set up your development environment** following the [Setup Guide](README.md#setup-guide)

## 🔄 Development Workflow

1. **Create a new branch** for your feature or bugfix:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b bugfix/issue-description
   ```

2. **Keep your branch up to date** with the upstream main branch:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

3. **Make your changes** following the coding standards below

4. **Test your changes** thoroughly

5. **Commit your changes** using conventional commit format (see below)

6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Open a Pull Request** against the main repository

## 📝 Coding Standards

### C# Code Style

- Follow the `.editorconfig` rules included in the repository
- Use **PascalCase** for class names, methods, and properties
- Use **camelCase** for local variables and parameters
- Use **_camelCase** for private fields (with underscore prefix)
- Add XML documentation comments to public APIs
- Keep methods focused and single-purpose (SRP)
- Prefer `async/await` over synchronous operations for I/O
- Use `DateTime.UtcNow` instead of `DateTime.Now` for consistency

### Architecture Guidelines

- Follow Clean Architecture principles
- Keep domain logic pure and framework-agnostic
- Use dependency injection for all services
- Implement repository pattern for data access
- Use FluentValidation for input validation
- Keep controllers thin - business logic belongs in services

### Naming Conventions

- **Controllers**: `{Entity}Controller.cs` (e.g., `UserController.cs`)
- **Services**: `I{Service}Service.cs` for interfaces, `{Service}Service.cs` for implementations
- **Repositories**: `I{Entity}Repository.cs` for interfaces
- **DTOs**: `{Purpose}{Entity}Dto.cs` (e.g., `RegisterRequestDto.cs`)
- **Validators**: `{Dto}Validator.cs` (e.g., `RegisterRequestDtoValidator.cs`)

## 📝 Commit Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

### Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, missing semicolons, etc.)
- `refactor`: Code refactoring without feature changes
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks, dependency updates

### Examples
```bash
feat(auth): add password reset functionality

Implement password reset flow with email verification.
- Add password reset token generation
- Add email template for reset link
- Add password reset endpoint

Closes #123
```

```bash
fix(user): resolve null reference in profile update

Add null check for avatar URL to prevent NullReferenceException
when updating user profile without uploading new avatar.

Fixes #456
```

## 🔍 Pull Request Process

1. **Update documentation** if you're adding new features
2. **Add or update tests** for your changes
3. **Ensure all tests pass** locally
4. **Update the README.md** if needed
5. **Fill out the PR template** with all relevant information
6. **Request review** from maintainers
7. **Address review feedback** promptly
8. **Squash commits** if requested before merging

### PR Checklist

Before submitting your PR, ensure:

- [ ] Code follows the project's coding standards
- [ ] All tests pass (`dotnet test`)
- [ ] Code builds without warnings (`dotnet build`)
- [ ] New features have appropriate tests
- [ ] Documentation is updated
- [ ] Commit messages follow conventional commits format
- [ ] PR description clearly describes the changes
- [ ] No sensitive information (API keys, passwords) is committed

## 🧪 Testing Guidelines

### Unit Tests

- Write unit tests for all business logic
- Use meaningful test names: `MethodName_Scenario_ExpectedBehavior`
- Follow AAA pattern: Arrange, Act, Assert
- Mock external dependencies
- Aim for high code coverage (>80%)

### Integration Tests

- Test API endpoints end-to-end
- Use test database (not production)
- Clean up test data after each test
- Test both success and failure scenarios

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test GearUp.Tests/GearUp.Tests.csproj
```

## 🔒 Security

- Never commit sensitive data (API keys, passwords, connection strings)
- Use environment variables for configuration
- Follow OWASP security best practices
- Report security vulnerabilities privately to the maintainers

## 📞 Questions?

If you have questions or need help:
- Open an issue for bugs or feature requests
- Start a discussion for general questions
- Contact the maintainers directly for security issues

## 📄 License

By contributing to GearUp, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to GearUp! 🚀
