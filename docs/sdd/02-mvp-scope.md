# MVP Scope

Priorities below apply CRM-24's MVP Scope Strategy verbatim: **P0 — must demonstrate working functionality**, **P1 — basic working implementation**, **P2 — simplified/demo implementation**.

| Requirement area | Feature bullet | Priority | Rationale (from CRM-24) | Included in 2-day MVP? |
|---|---|---|---|---|
| Security & Administration | Authentication | P0 | Explicit P0 item in MVP Scope Strategy (A-13) | Yes |
| Customer Management | Customer profiles | P0 | "Customer management" is explicit P0 (A-02) | Yes |
| Customer Management | Contact details | P0 | "Customer management" is explicit P0 (A-02) | Yes |
| Customer Management | Interaction history | P0 | "Customer management" is explicit P0 (A-02) | Yes |
| Customer Management | Notes and attachments | P0 | "Customer management" is explicit P0 (A-02); details in OQ-08 | Yes |
| Ticket Management | Create and track tickets | P0 | "Ticket management" is explicit P0 (A-03) | Yes |
| Ticket Management | Categories and priorities | P0 | "Ticket management" is explicit P0 (A-03); enum vs configurable in OQ-10 | Yes |
| Ticket Management | Assign tickets to agents | P0 | "Ticket management" is explicit P0 (A-03) | Yes |
| Ticket Management | Status and escalation | P0 | "Ticket management" is explicit P0 (A-03); escalation detail in OQ-11 | Yes |
| Ticket Management | Ticket history | P0 | "Ticket management" is explicit P0 (A-03) | Yes |
| Agent Dashboard | Assigned tickets | P0 | "Agent dashboard" is explicit P0 (A-04) | Yes |
| Agent Dashboard | Customer information | P0 | "Agent dashboard" is explicit P0 (A-04) | Yes |
| Agent Dashboard | Tasks and reminders | P0 | "Agent dashboard" is explicit P0 (A-04) | Yes |
| Agent Dashboard | Quick replies | P0 | "Agent dashboard" is explicit P0 (A-04) | Yes |
| Agent Dashboard | Team collaboration | P0 | "Agent dashboard" is explicit P0 (A-04) | Yes |
| SLA & Automation | Response and resolution targets | P0 | "Basic SLA" is explicit P0 (A-05); targets in OQ-03 | Yes |
| SLA & Automation | Alerts and notifications | P0 | "Basic SLA" is explicit P0 (A-05) | Yes |
| SLA & Automation | Automatic assignment | P2 | Assumed under "Advanced automation" P2 (A-05); pending OQ-11 | Simplified/demo only |
| SLA & Automation | Escalation rules | P2 | Assumed under "Advanced automation" P2 (A-05); pending OQ-11 | Simplified/demo only |
| AI Features | Ticket summaries | P0 | "AI assistance" is explicit P0 (A-07); provider in OQ-04 | Yes |
| AI Features | Suggested replies | P0 | "AI assistance" is explicit P0 (A-07); provider in OQ-04 | Yes |
| AI Features | Automatic categorization | P0 | "AI assistance" is explicit P0 (A-07); provider in OQ-04 | Yes |
| AI Features | Suggested solutions | P0 | "AI assistance" is explicit P0 (A-07); provider in OQ-04 | Yes |
| AI Features | AI chatbot | P0 | "AI assistance" is explicit P0 (A-07); provider in OQ-04 | Yes |
| Platform | Arabic and English | P0 | "Arabic/English support" is explicit P0; RTL/i18n scope in OQ-07 | Yes |
| Platform | Web and mobile friendly | P0 | "Responsive UI" is explicit P0; mobile scope narrowed to responsive web by A-01 | Yes |
| Security & Administration | Users and roles | P0 | "Basic administration" is explicit P0 (A-10); role list in OQ-02 | Yes |
| Security & Administration | Permissions | P0 | "Basic administration" is explicit P0 (A-10); role list in OQ-02 | Yes |
| Security & Administration | System configuration | P0 | "Basic administration" is explicit P0 (A-10) | Yes |
| Knowledge Base | FAQs | P1 | "Knowledge Base" is explicit P1 (A-06) | Basic implementation |
| Knowledge Base | Help articles | P1 | "Knowledge Base" is explicit P1 (A-06) | Basic implementation |
| Knowledge Base | Solutions and guides | P1 | "Knowledge Base" is explicit P1 (A-06) | Basic implementation |
| Knowledge Base | Search | P1 | "Knowledge Base" is explicit P1 (A-06) | Basic implementation |
| Customer Portal | Submit tickets | P1 | "Customer Portal" is explicit P1 (A-08); auth in OQ-12 | Basic implementation |
| Customer Portal | Track requests | P1 | "Customer Portal" is explicit P1 (A-08) | Basic implementation |
| Customer Portal | View history | P1 | "Customer Portal" is explicit P1 (A-08) | Basic implementation |
| Customer Portal | Access FAQs | P1 | "Customer Portal" is explicit P1 (A-08) | Basic implementation |
| Customer Portal | Submit feedback | P1 | "Customer Portal" is explicit P1 (A-08) | Basic implementation |
| Reports & Management | Ticket reports | P1 | "Reports" is explicit P1 (A-09); detail in OQ-13 | Basic implementation |
| Reports & Management | SLA performance | P1 | "Reports" is explicit P1 (A-09); detail in OQ-13 | Basic implementation |
| Reports & Management | Agent performance | P1 | "Reports" is explicit P1 (A-09); detail in OQ-13 | Basic implementation |
| Reports & Management | Customer satisfaction | P1 | "Reports" is explicit P1 (A-09); detail in OQ-13 | Basic implementation |
| Reports & Management | Management dashboards | P1 | Assumed P1 under "Reports" (A-09); may overlap "Advanced reporting" P2 | Basic implementation |
| Security & Administration | Audit logs | P1 | Assumed P1 pending retention/immutability answer (A-10, OQ-14) | Basic implementation |
| Communication Channels | Email | P1 | "Communication channel representation" is explicit P1 (A-14); P0-vs-P1 ambiguity in OQ-09 | Basic implementation |
| Communication Channels | WhatsApp | P1 | "Communication channel representation" is explicit P1 (A-14) | Basic implementation |
| Communication Channels | Live chat | P1 | "Communication channel representation" is explicit P1 (A-14) | Basic implementation |
| Communication Channels | SMS | P1 | "Communication channel representation" is explicit P1 (A-14) | Basic implementation |
| Communication Channels | Web forms | P1 | "Communication channel representation" is explicit P1 (A-14) | Basic implementation |
| Platform | Multi-department | P1 | Not explicitly classified; assumed P1 (A-12); isolation semantics in OQ-06 | Basic implementation |
| Platform | Multi-branch | P1 | Not explicitly classified; assumed P1 (A-12); isolation semantics in OQ-06 | Basic implementation |
| Integrations | APIs | P2 | Assumed under "External integrations" P2 (A-11) | Simplified/demo only |
| Integrations | ERP | P2 | Maps to "ERP integration", explicit P2 (A-11) | Simplified/demo only |
| Integrations | Email, SMS and WhatsApp | P2 | Maps to "Real WhatsApp/SMS provider integration", explicit P2 (A-11) | Simplified/demo only |
| Integrations | External systems | P2 | Assumed under "External integrations" P2 (A-11) | Simplified/demo only |
| Platform | Custom branding | P2 | Maps to "Advanced branding", explicit P2; scope in OQ-16 | Simplified/demo only |

**P2 functionality must not block the core CRM workflow.**
