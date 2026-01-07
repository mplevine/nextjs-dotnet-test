# System Architecture Planning Prompt

> If you’ve already answered the key questions, see the concrete outline in `system-architecture-outline.md`.

## Purpose/Overview

- Help me plan the architecture for a new system.
- This system will consist of a backend API (ICS API) and a frontend web application (ICS Admin Interface).
- The purpose of this system is to help me understand the best practices for:
  - Building a Next.js web application that utilizes a .NET API.
  - Implementing authentication and authorization using Microsoft Entra ID.
  - Registering and configuring applications in Microsoft Entra ID.
  - Deploying both components to an on-premise IIS server.

## Technical Requirements

- **IMPORTANT: This system is inteneded to serve as a simple example for learning purposes. Do not overcomplicate the architecture or implementation details.**
- The ICS API should be built using .NET C# and follow RESTful principles.
- The ICS Admin Interface should be built using Next.js and should consume the ICS API.
- Both components should be designed so that they can be easily deployed to an on-premise IIS server.
- The system must utilize Microsoft Entra ID for authentication and authorization.
  - Authentication should be handled via OAuth 2.0 and OpenID Connect.
  - Authorization should be role-based, with roles defined in Microsoft Entra ID.
    - There will be two roles: Admin and Attorney.
- The ICS Admin Interface should only be accessible to users with the Admin role.
- The ICS API should be accessible to both Admin and Attorney roles, but with different levels of access based on their roles.
- Both the ICS API and the ICS Admin Interface should implement proper error handling and logging.

## Instructions
  
- Prompt me with questions to gather more information about the system requirements, constraints, and any other relevant details.
  - For example:
    - Help me decide whether or not the Next.js application should use server-side rendering (SSR) or static site generation (SSG).
    - Help me decide on how the Next.js application should communicate with the .NET API (e.g., using fetch, Axios, etc.).
- Based on the information provided, help me outline the architecture for both the ICS API and the ICS Admin Interface.
- Provide recommendations for best practices in building and deploying the system.
- Suggest any additional tools, libraries, or frameworks that may be beneficial for the development and deployment of the system.

## Outputs

- `planning-answers.md`: captured answers
- `../docs/system-architecture-outline.md`: recommended target architecture
