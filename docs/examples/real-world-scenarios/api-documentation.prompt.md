---
name: "api-documentation-generator"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.6
  maxOutputTokens: 5000

input:
  schema:
    documentation_format:
      type: string
      enum: ["openapi", "markdown", "both"]
      description: "Format for API documentation"
      default: "both"
    include_examples:
      type: boolean
      description: "Include request/response examples"
      default: true
    generate_postman_collection:
      type: boolean
      description: "Generate Postman collection for testing"
      default: true
    include_authentication_docs:
      type: boolean
      description: "Include authentication and authorization documentation"
      default: true
---

# API Documentation Generator

Generate comprehensive API documentation for .NET Web APIs with examples, testing collections, and integration guides.

## Documentation Configuration
- **Format**: {{documentation_format}}
- **Include Examples**: {{#if include_examples}}Yes{{else}}No{{/if}}
- **Postman Collection**: {{#if generate_postman_collection}}Yes{{else}}No{{/if}}
- **Authentication Docs**: {{#if include_authentication_docs}}Yes{{else}}No{{/if}}

## API Discovery and Analysis

### 1. API Surface Analysis
- Discover all controllers and action methods
- Analyze route patterns and HTTP verbs
- Identify parameter types and validation attributes
- Document return types and status codes
- Map authentication and authorization requirements

### 2. Model Analysis
- Analyze request and response models
- Document model validation rules and constraints
- Identify nested objects and relationships
- Review enum types and their values
- Document model inheritance and polymorphism

### 3. Authentication and Authorization
{{#if include_authentication_docs}}
- Analyze authentication mechanisms (JWT, OAuth, API Keys)
- Document authorization policies and requirements
- Review role-based and claims-based access control
- Identify protected endpoints and permissions
{{/if}}

### 4. Configuration and Environment Analysis
- Review API versioning strategy
- Analyze CORS configuration and policies
- Document rate limiting and throttling
- Review error handling and exception policies

## Documentation Generation

{{#contains documentation_format "openapi"}}
### OpenAPI/Swagger Documentation
Generate comprehensive OpenAPI 3.0 specification:

#### API Metadata
- API title, version, and description
- Server configuration and base URLs
- Contact information and license details
- Tags and category organization

#### Endpoint Documentation
- Complete endpoint definitions with parameters
- Request and response schema definitions
- HTTP status code documentation
- Authentication and security requirements

#### Schema Definitions
- Complete model schemas with validation rules
- Example values and descriptions
- Enum definitions and allowed values
- Inheritance and composition relationships
{{/contains}}

{{#contains documentation_format "markdown"}}
### Markdown Documentation
Create comprehensive markdown documentation:

#### API Overview
- Introduction and purpose of the API
- Getting started guide and quick examples
- Authentication and authorization overview
- Base URLs and environment information

#### Endpoint Reference
- Organized by controller or functional area
- Detailed parameter descriptions
- Request and response examples
- Error handling and status codes

#### Integration Guide
- Step-by-step integration instructions
- Common use cases and workflows
- Best practices and recommendations
- Troubleshooting and FAQ section
{{/contains}}

{{#if include_examples}}
### Request/Response Examples
Generate realistic examples for all endpoints:

#### Request Examples
- Complete request examples with headers
- Parameter examples with realistic data
- Authentication examples and token usage
- Content-Type and Accept header examples

#### Response Examples
- Success response examples with real data
- Error response examples with proper error codes
- Pagination examples where applicable
- Different content-type response examples
{{/if}}

{{#if generate_postman_collection}}
### Postman Collection Generation
Create comprehensive Postman collection:

#### Collection Structure
- Organized by controller or functional area
- Environment variables for different environments
- Authentication configuration and tokens
- Pre-request scripts for common setup

#### Request Configuration
- Complete request setup with parameters
- Example request bodies with realistic data
- Response validation tests
- Variable extraction from responses

#### Testing Scripts
- Automated tests for successful responses
- Validation of response structure and data
- Authentication token management
- Environment-specific configuration
{{/if}}

## Documentation Organization

### File Structure
Create organized documentation in `docs/api/`:

```
docs/api/
├── README.md                 # API overview and getting started
├── authentication.md         # Authentication and authorization
├── endpoints/               # Detailed endpoint documentation
│   ├── users.md
│   ├── products.md
│   └── orders.md
├── models/                  # Data model documentation
│   ├── user-models.md
│   ├── product-models.md
│   └── common-models.md
├── examples/               # Request/response examples
│   ├── user-examples.md
│   └── product-examples.md
├── integration/            # Integration guides
│   ├── getting-started.md
│   ├── authentication-guide.md
│   └── error-handling.md
└── openapi/               # OpenAPI specifications
    ├── openapi.yaml
    └── openapi.json
```

### Content Standards
Ensure all documentation includes:
- ✅ **Clear descriptions** for all endpoints and parameters
- ✅ **Realistic examples** with proper data types
- ✅ **Error scenarios** with appropriate status codes
- ✅ **Authentication details** where required
- ✅ **Validation rules** and constraints
- ✅ **Rate limiting** and usage guidelines

## Interactive Documentation

### Swagger UI Setup
{{#contains documentation_format "openapi"}}
- Configure Swagger UI for interactive testing
- Set up authentication flows for testing
- Include example values and try-it-out functionality
- Configure for different environments (dev, staging, prod)
{{/contains}}

### Documentation Website
- Create a documentation website structure
- Include navigation and search functionality
- Provide downloadable resources (OpenAPI specs, Postman collections)
- Set up automatic updates with CI/CD integration

## Testing and Validation

### Documentation Accuracy
- Validate all examples against actual API responses
- Test authentication flows and requirements
- Verify parameter validation and error responses
- Confirm status codes and response formats

### Postman Collection Testing
{{#if generate_postman_collection}}
- Test all requests in the Postman collection
- Validate authentication and token management
- Verify environment variable usage
- Test error scenarios and edge cases
{{/if}}

### Integration Testing
- Test integration guide steps with fresh setup
- Validate code examples and snippets
- Confirm authentication setup instructions
- Test troubleshooting procedures and solutions

## Output Deliverables

### 📚 Complete API Documentation
{{#eq documentation_format "both"}}
- OpenAPI 3.0 specification (YAML and JSON formats)
- Comprehensive markdown documentation
- Interactive Swagger UI configuration
{{/eq}}
{{#eq documentation_format "openapi"}}
- OpenAPI 3.0 specification (YAML and JSON formats)
- Interactive Swagger UI configuration
{{/eq}}
{{#eq documentation_format "markdown"}}
- Comprehensive markdown documentation
- Structured documentation website
{{/eq}}

{{#if include_examples}}
### 💡 Examples and Samples
- Complete request/response examples for all endpoints
- Code samples in multiple programming languages
- Authentication flow examples
- Error handling examples
{{/if}}

{{#if generate_postman_collection}}
### 🧪 Testing Resources
- Complete Postman collection with all endpoints
- Environment configurations for different stages
- Automated test scripts and validations
- Documentation for collection usage
{{/if}}

{{#if include_authentication_docs}}
### 🔐 Security Documentation
- Complete authentication and authorization guide
- Security best practices and recommendations
- Token management and refresh procedures
- Security testing guidelines
{{/if}}

### 🚀 Integration Resources
- Step-by-step integration guides
- SDK documentation (if available)
- Common use case implementations
- Troubleshooting guides and FAQ

## Maintenance and Updates

### Documentation Lifecycle
- Establish documentation update procedures
- Link documentation updates to code changes
- Set up automated documentation generation where possible
- Create review processes for documentation changes

### Continuous Integration
- Integrate documentation generation into CI/CD pipeline
- Automate OpenAPI specification generation
- Set up documentation deployment and hosting
- Implement documentation quality checks

### Community and Feedback
- Set up feedback collection mechanisms
- Create contribution guidelines for documentation
- Establish community review processes
- Plan for documentation internationalization if needed

## Quality Assurance

### Documentation Review Checklist
- ✅ All endpoints documented with complete information
- ✅ Examples are accurate and tested
- ✅ Authentication requirements clearly specified
- ✅ Error responses properly documented
- ✅ Model schemas complete with validation rules
- ✅ Integration guides tested and verified

### User Experience Validation
- Test documentation with new developers
- Validate that examples work as documented
- Ensure navigation and organization are intuitive
- Confirm that troubleshooting guides resolve common issues

This comprehensive API documentation package provides everything needed for developers to successfully integrate with and use the API, including interactive testing capabilities and comprehensive examples.