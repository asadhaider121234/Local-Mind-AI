import requests
import json

url = "http://127.0.0.1:8000/index"
payload = {"folder_path": r"c:\Users\user\Desktop\FYP Project\LOCAL MIND AI\test_docs"}
headers = {"Content-Type": "application/json"}

try:
    response = requests.post(url, data=json.dumps(payload), headers=headers)
    print(response.json())
except Exception as e:
    print(f"Error: {e}")
