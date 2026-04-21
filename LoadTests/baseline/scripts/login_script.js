
import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    stages: [
        { duration: '1m', target: 20 },
        { duration: '2m', target: 50 },
        { duration: '3m', target: 100 },
        { duration: '2m', target: 100 },
        { duration: '1m', target: 0 },
    ],
};

const BASE_URL = 'http://localhost:5255';
const TEST_USER = 'kevon25';
const TEST_PASSWORD = 'Password123!';

export default function () {
    // 🔹 STEP 1: LOGIN every iteration
    const loginPayload = JSON.stringify({
        UsernameOrEmail: TEST_USER,
        Password: TEST_PASSWORD,
    });

    const loginRes = http.post(
        `${BASE_URL}/api/v1/auth/login`,
        loginPayload,
        {
            headers: { 'Content-Type': 'application/json' },
        }
    );

    check(loginRes, {
        'login status 200': (r) => r.status === 200,
    });

    if (loginRes.status !== 200) {
        sleep(1);
        return;
    }

    sleep(0.2);
}
