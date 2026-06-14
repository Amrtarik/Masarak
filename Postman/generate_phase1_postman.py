import json
import os
import uuid

def create_request(name, method, url_path, auth_type=None, auth_token_var=None, body=None, tests=None, prerequest=None):
    req = {
        "name": name,
        "request": {
            "method": method,
            "header": [
                {
                    "key": "Content-Type",
                    "value": "application/json",
                    "type": "text"
                }
            ],
            "url": {
                "raw": "{{baseUrl}}" + url_path,
                "host": ["{{baseUrl}}"],
                "path": url_path.strip("/").split("/")
            }
        },
        "response": [],
        "event": []
    }
    
    if auth_type == "bearer":
        req["request"]["auth"] = {
            "type": "bearer",
            "bearer": [
                {
                    "key": "token",
                    "value": "{{" + auth_token_var + "}}",
                    "type": "string"
                }
            ]
        }

    if body:
        req["request"]["body"] = {
            "mode": "raw",
            "raw": json.dumps(body, indent=4)
        }
        
    if prerequest:
        req["event"].append({
            "listen": "prerequest",
            "script": {
                "exec": prerequest,
                "type": "text/javascript"
            }
        })

    if tests:
        req["event"].append({
            "listen": "test",
            "script": {
                "exec": tests,
                "type": "text/javascript"
            }
        })
        
    # Remove event array if empty
    if not req["event"]:
        del req["event"]

    return req

def get_status_test(code):
    return [
        f'pm.test("Status code is {code} or handled error", function () {{',
        f'    pm.expect(pm.response.code).to.be.oneOf([{code}, 400, 401, 403, 404]);',
        '});'
    ]

collection = {
    "info": {
        "name": "Masarak_Phase1_Subscriptions",
        "description": "Postman testing suite for Masarak Phase 1: Identity, Access, and Subscriptions.",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [
        {
            "name": "Plans",
            "item": [
                create_request("Get All Plans", "GET", "/api/plans", tests=get_status_test(200))
            ]
        },
        {
            "name": "Subscriptions - Authenticated",
            "item": [
                create_request("Initiate Checkout", "POST", "/api/subscriptions/checkout", auth_type="bearer", auth_token_var="studentAccessToken", body={"planId": 1}, tests=get_status_test(200)),
                create_request("Get Active Subscription", "GET", "/api/subscriptions/me", auth_type="bearer", auth_token_var="studentAccessToken", tests=get_status_test(200)),
                create_request("Get Subscription History", "GET", "/api/subscriptions/me/history", auth_type="bearer", auth_token_var="studentAccessToken", tests=get_status_test(200))
            ]
        },
        {
            "name": "Subscriptions - Webhook",
            "item": [
                create_request("Handle Stripe Webhook", "POST", "/api/subscriptions/webhook", body={"type": "checkout.session.completed", "data": {"object": {"metadata": {"userId": "user-1", "planId": "1"}}}}, tests=get_status_test(200))
            ]
        },
        {
            "name": "Subscriptions - Admin",
            "item": [
                create_request("Admin Activate Subscription", "POST", "/api/subscriptions/admin/activate", auth_type="bearer", auth_token_var="adminAccessToken", body={"userId": "user-1", "planId": 1, "status": "Active"}, tests=get_status_test(200)),
                create_request("Admin Cancel Subscription", "POST", "/api/subscriptions/admin/cancel/1", auth_type="bearer", auth_token_var="adminAccessToken", tests=get_status_test(200)),
                create_request("Get All Subscriptions", "GET", "/api/admin/subscriptions", auth_type="bearer", auth_token_var="adminAccessToken", tests=get_status_test(200))
            ]
        },
        {
            "name": "Parent Linking",
            "item": [
                create_request("Link Parent to Student", "POST", "/api/parent/link-student", auth_type="bearer", auth_token_var="parentAccessToken", body={"studentId": "some-student-id"}, tests=get_status_test(200)),
                create_request("Get Linked Students", "GET", "/api/parent/linked-students", auth_type="bearer", auth_token_var="parentAccessToken", tests=get_status_test(200))
            ]
        }
    ]
}

environment = {
    "id": str(uuid.uuid4()),
    "name": "Masarak_Local",
    "values": [
        {"key": "baseUrl", "value": "http://localhost:5000", "type": "default", "enabled": True},
        {"key": "adminAccessToken", "value": "", "type": "default", "enabled": True},
        {"key": "parentAccessToken", "value": "", "type": "default", "enabled": True},
        {"key": "studentAccessToken", "value": "", "type": "default", "enabled": True}
    ],
    "_postman_variable_scope": "environment"
}

postman_dir = r"c:\Users\hbeba\source\repos\Masarak\Postman"
os.makedirs(postman_dir, exist_ok=True)

with open(os.path.join(postman_dir, "Masarak_Phase1.postman_collection.json"), "w") as f:
    json.dump(collection, f, indent=4)

with open(os.path.join(postman_dir, "Masarak_Phase1.postman_environment.json"), "w") as f:
    json.dump(environment, f, indent=4)

print("Phase 1 Files generated.")
