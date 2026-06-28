const fs = require('fs');

const collection = {
    info: {
        name: "Masarak_Phase5",
        schema: "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        description: "Postman collection for Masarak Phase 5 - AI Recommendations, Smart Reports & Analytics"
    },
    variable: [
        { key: "baseUrl", value: "https://localhost:49179", type: "string" },
        { key: "studentToken", value: "{{studentToken}}", type: "string" },
        { key: "teacherToken", value: "{{teacherToken}}", type: "string" },
        { key: "parentToken", value: "{{parentToken}}", type: "string" },
        { key: "adminToken", value: "{{adminToken}}", type: "string" }
    ],
    item: [
        {
            name: "1. Authentication (Setup)",
            item: [
                {
                    name: "Login Admin",
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "var jsonData = pm.response.json();",
                                    "if(jsonData.token) { pm.environment.set(\"adminToken\", jsonData.token); }"
                                ]
                            }
                        }
                    ],
                    request: {
                        method: "POST",
                        header: [ { key: "Content-Type", value: "application/json" } ],
                        url: { raw: "{{baseUrl}}/api/auth/login", host: ["{{baseUrl}}"], path: ["api", "auth", "login"] },
                        body: { mode: "raw", raw: JSON.stringify({ email: "admin@masarak.com", password: "Password123!" }) }
                    }
                },
                {
                    name: "Login Teacher",
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "var jsonData = pm.response.json();",
                                    "if(jsonData.token) { pm.environment.set(\"teacherToken\", jsonData.token); }"
                                ]
                            }
                        }
                    ],
                    request: {
                        method: "POST",
                        header: [ { key: "Content-Type", value: "application/json" } ],
                        url: { raw: "{{baseUrl}}/api/auth/login", host: ["{{baseUrl}}"], path: ["api", "auth", "login"] },
                        body: { mode: "raw", raw: JSON.stringify({ email: "teacher@masarak.com", password: "Password123!" }) }
                    }
                },
                {
                    name: "Login Student",
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "var jsonData = pm.response.json();",
                                    "if(jsonData.token) { pm.environment.set(\"studentToken\", jsonData.token); }"
                                ]
                            }
                        }
                    ],
                    request: {
                        method: "POST",
                        header: [ { key: "Content-Type", value: "application/json" } ],
                        url: { raw: "{{baseUrl}}/api/auth/login", host: ["{{baseUrl}}"], path: ["api", "auth", "login"] },
                        body: { mode: "raw", raw: JSON.stringify({ email: "student@masarak.com", password: "Password123!" }) }
                    }
                },
                {
                    name: "Login Parent",
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "var jsonData = pm.response.json();",
                                    "if(jsonData.token) { pm.environment.set(\"parentToken\", jsonData.token); }"
                                ]
                            }
                        }
                    ],
                    request: {
                        method: "POST",
                        header: [ { key: "Content-Type", value: "application/json" } ],
                        url: { raw: "{{baseUrl}}/api/auth/login", host: ["{{baseUrl}}"], path: ["api", "auth", "login"] },
                        body: { mode: "raw", raw: JSON.stringify({ email: "parent@masarak.com", password: "Password123!" }) }
                    }
                }
            ]
        },
        {
            name: "2. Student Insights",
            item: [
                {
                    name: "Get Learning Insights Dashboard",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{studentToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/student/insights?academicYear=2026",
                            host: ["{{baseUrl}}"],
                            path: ["api", "student", "insights"],
                            query: [ { key: "academicYear", value: "2026" } ]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Subject Recommendations",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{studentToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/student/recommendations/{{subjectId}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "student", "recommendations", "{{subjectId}}"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Subject Recommendations (Unauthorized - 401)",
                    request: {
                        method: "GET",
                        url: {
                            raw: "{{baseUrl}}/api/student/recommendations/{{subjectId}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "student", "recommendations", "{{subjectId}}"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 401', function() { pm.response.to.have.status(401); });"
                                ]
                            }
                        }
                    ]
                }
            ]
        },
        {
            name: "3. Parent Reports & Alerts",
            item: [
                {
                    name: "Generate Parent Report",
                    request: {
                        method: "POST",
                        header: [
                            { key: "Authorization", value: "Bearer {{parentToken}}" },
                            { key: "Content-Type", value: "application/json" }
                        ],
                        url: {
                            raw: "{{baseUrl}}/api/parent/reports/{{studentId}}/generate",
                            host: ["{{baseUrl}}"],
                            path: ["api", "parent", "reports", "{{studentId}}", "generate"]
                        },
                        body: {
                            mode: "raw",
                            raw: JSON.stringify({ reportMonth: "October-2026" })
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200 or 201', function() { pm.expect(pm.response.code).to.be.oneOf([200, 201]); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Cached Parent Report",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{parentToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/parent/reports/{{studentId}}/October-2026",
                            host: ["{{baseUrl}}"],
                            path: ["api", "parent", "reports", "{{studentId}}", "October-2026"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200 or 404', function() { pm.expect(pm.response.code).to.be.oneOf([200, 404]); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Student Alerts",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{parentToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/parent/children/{{studentId}}/alerts",
                            host: ["{{baseUrl}}"],
                            path: ["api", "parent", "children", "{{studentId}}", "alerts"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                }
            ]
        },
        {
            name: "4. Teacher Analytics",
            item: [
                {
                    name: "Get Class Analytics Dashboard",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{teacherToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/teacher/analytics/{{classId}}/{{subjectId}}?academicYear=2026",
                            host: ["{{baseUrl}}"],
                            path: ["api", "teacher", "analytics", "{{classId}}", "{{subjectId}}"],
                            query: [ { key: "academicYear", value: "2026" } ]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Student Insight",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{teacherToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/teacher/students/{{studentId}}/insights/{{subjectId}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "teacher", "students", "{{studentId}}", "insights", "{{subjectId}}"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Generate Teaching Suggestion",
                    request: {
                        method: "POST",
                        header: [ { key: "Authorization", value: "Bearer {{teacherToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/teacher/students/{{studentId}}/suggestions/{{subjectId}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "teacher", "students", "{{studentId}}", "suggestions", "{{subjectId}}"]
                        },
                        body: {
                            mode: "raw",
                            raw: ""
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                }
            ]
        },
        {
            name: "5. Admin Analytics",
            item: [
                {
                    name: "Get Platform Analytics",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{adminToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/admin/analytics/platform?academicYear=2026",
                            host: ["{{baseUrl}}"],
                            path: ["api", "admin", "analytics", "platform"],
                            query: [ { key: "academicYear", value: "2026" } ]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Grade Heatmap",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{adminToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/admin/analytics/grades/{{gradeId}}/heatmap?academicYear=2026",
                            host: ["{{baseUrl}}"],
                            path: ["api", "admin", "analytics", "grades", "{{gradeId}}", "heatmap"],
                            query: [ { key: "academicYear", value: "2026" } ]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Get Prompt Templates",
                    request: {
                        method: "GET",
                        header: [ { key: "Authorization", value: "Bearer {{adminToken}}" } ],
                        url: {
                            raw: "{{baseUrl}}/api/admin/ai/prompt-templates",
                            host: ["{{baseUrl}}"],
                            path: ["api", "admin", "ai", "prompt-templates"]
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 200', function() { pm.response.to.have.status(200); });",
                                    "var jsonData = pm.response.json();",
                                    "if(jsonData.length > 0) { pm.environment.set('templateKey', jsonData[0].key); }"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Update Prompt Template",
                    request: {
                        method: "PUT",
                        header: [
                            { key: "Authorization", value: "Bearer {{adminToken}}" },
                            { key: "Content-Type", value: "application/json" }
                        ],
                        url: {
                            raw: "{{baseUrl}}/api/admin/ai/prompt-templates/{{templateKey}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "admin", "ai", "prompt-templates", "{{templateKey}}"]
                        },
                        body: {
                            mode: "raw",
                            raw: JSON.stringify({
                                systemPrompt: "You are an educational assistant for Egyptian K-12 curriculum.",
                                userPromptTemplate: "Please analyze {student_name} results: {results}",
                                maxTokens: 1000,
                                temperature: 0.7
                            })
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 204 or 404', function() { pm.expect(pm.response.code).to.be.oneOf([204, 404]); });"
                                ]
                            }
                        }
                    ]
                },
                {
                    name: "Update Prompt Template (Forbidden - Teacher)",
                    request: {
                        method: "PUT",
                        header: [
                            { key: "Authorization", value: "Bearer {{teacherToken}}" },
                            { key: "Content-Type", value: "application/json" }
                        ],
                        url: {
                            raw: "{{baseUrl}}/api/admin/ai/prompt-templates/{{templateKey}}",
                            host: ["{{baseUrl}}"],
                            path: ["api", "admin", "ai", "prompt-templates", "{{templateKey}}"]
                        },
                        body: {
                            mode: "raw",
                            raw: JSON.stringify({
                                systemPrompt: "Hack attempt",
                                userPromptTemplate: "Hack",
                                maxTokens: 1000,
                                temperature: 0.7
                            })
                        }
                    },
                    event: [
                        {
                            listen: "test",
                            script: {
                                exec: [
                                    "pm.test('Status code is 403', function() { pm.response.to.have.status(403); });"
                                ]
                            }
                        }
                    ]
                }
            ]
        }
    ]
};

const environment = {
    name: "Masarak_Phase5_Environment",
    values: [
        { key: "baseUrl", value: "https://localhost:49179", type: "default", enabled: true },
        { key: "adminToken", value: "", type: "default", enabled: true },
        { key: "teacherToken", value: "", type: "default", enabled: true },
        { key: "studentToken", value: "", type: "default", enabled: true },
        { key: "parentToken", value: "", type: "default", enabled: true },
        { key: "studentId", value: "3", type: "default", enabled: true },
        { key: "subjectId", value: "1", type: "default", enabled: true },
        { key: "classId", value: "1", type: "default", enabled: true },
        { key: "gradeId", value: "1", type: "default", enabled: true },
        { key: "templateKey", value: "weakness_analysis", type: "default", enabled: true }
    ]
};

fs.writeFileSync('d:/ITI/GradProj/Masarak/Postman/Masarak_Phase5.postman_collection.json', JSON.stringify(collection, null, 2));
fs.writeFileSync('d:/ITI/GradProj/Masarak/Postman/Masarak_Phase5.postman_environment.json', JSON.stringify(environment, null, 2));

console.log('Successfully generated Postman collection and environment for Phase 5.');
