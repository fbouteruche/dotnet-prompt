---
name: "quick-quality-check"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.4
  maxOutputTokens: 3000
---

# Quick Quality Check

Perform a rapid quality assessment of the .NET project to identify immediate issues and improvements.

## Quality Assessment Areas

### 1. Build Health
- Attempt to build the project
- Identify any build errors or warnings
- Check if the project builds successfully in Release configuration

### 2. Project Structure Quality
- Evaluate project organization and file structure
- Check for proper separation of concerns
- Identify any obvious architectural issues

### 3. Code Quality Indicators
- Review naming conventions and coding standards adherence
- Look for potential code smells or anti-patterns
- Check for proper error handling patterns

### 4. Configuration Review
- Examine configuration files for best practices
- Check for hardcoded values that should be configurable
- Verify security considerations in configuration

### 5. Test Presence
- Check if unit tests exist and are properly organized
- If tests exist, run them to verify they pass
- Assess test coverage if information is available

## Report Generation

Create a quality report called `quality-check-report.md` with:

### ✅ Strengths Found
- What the project does well
- Good practices observed
- Quality indicators that are positive

### ⚠️ Issues Identified
- Build problems (if any)
- Code quality concerns
- Configuration issues
- Missing or failing tests

### 💡 Quick Wins
- Easy improvements that can be made immediately
- Simple fixes for identified issues
- Low-effort, high-impact improvements

### 📋 Recommendations
- Prioritized list of improvements
- Longer-term architectural suggestions
- Best practices to adopt

### 📊 Summary Score
- Overall quality rating (1-10 scale)
- Brief justification for the score
- Key factors influencing the rating

Keep the report concise but actionable - focus on items that developers can act on immediately.