# McpDemo.Api — Claude Code Instructions

## Project Overview
.NET 10 REST API (Product Catalog). Uses Entity Framework Core with SQL Server,
OpenTelemetry for observability (traces, metrics, logs exported to Elastic APM),
and Swagger for API documentation.

## Key Project Paths
- API project: `McpDemo.Api/`
- Controllers: `McpDemo.Api/Controllers/`
- Data/EF Core: `McpDemo.Api/Data/`
- Telemetry: `McpDemo.Api/Telemetry/`

## GitHub Workflow
- Default branch: `main`
- Branch naming: `fix/sonar-{date}` for SonarQube fixes, `feat/{description}` for features
- Always create PRs against `main`
- PR titles should follow conventional commits format e.g. `fix:`, `feat:`, `chore:`

## SonarQube Fix Workflow
When asked to fix SonarQube issues:
1. Use the SonarQube MCP to fetch all open issues for the `McpDemo.Api` project
2. Group issues by rule type before fixing
3. Fix each issue following .NET 10 best practices
4. Use the GitHub MCP to create a branch called `fix/sonar-{today's date}`
5. Commit fixes with one descriptive commit message per rule group
6. Open a PR against main with a description that lists every rule fixed,
   the number of instances, and a brief explanation of the change made
7. If an issue cannot be safely auto-fixed, add it as a comment on the PR
   flagging it for manual review

## Code Style
- Follow existing patterns in the codebase
- Do not remove or alter OpenTelemetry instrumentation
- Do not change connection string structure in appsettings.json
- Preserve existing Swagger documentation attributes on controllers
```

---

## Then Trigger the Workflow With a Short Prompt

Once `CLAUDE.md` is in place, your Step 4 prompt becomes as simple as:
```
Run the SonarQube fix workflow.