/*
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

export function setup() {
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
        'setup login status 200': (r) => r.status === 200,
    });

    if (loginRes.status !== 200) {
        throw new Error(`Setup login failed with status ${loginRes.status}`);
    }

    try {
        return {
            token: JSON.parse(loginRes.body).data.accessToken,
        };
    } catch {
        throw new Error('Setup login response did not include an access token');
    }
}

export default function (data) {
    const token = data?.token;
    if (!token) {
        return;
    }


    const res = http.get(`${BASE_URL}/api/v1/cars`, {
        headers: {
            Authorization: `Bearer ${token}`,
        },
    });

    check(res, {
        'status is 200': (r) => r.status === 200,
        'response has data': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body.isSuccess === true || body.IsSuccess === true;
            } catch {
                return false;
            }
        },
    });

    sleep(0.2);
}
*/

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

    let token;
    try {
        token = JSON.parse(loginRes.body).data.accessToken;
    } catch {
        return;
    }

    // 🔹 STEP 2: CALL protected endpoint
    const res = http.get(`${BASE_URL}/api/v1/posts`, {
        headers: {
            Authorization: `Bearer ${token}`,
        },
    });

    check(res, {
        'cars status 200': (r) => r.status === 200,
        'response has data': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body.isSuccess === true || body.IsSuccess === true;
            } catch {
                return false;
            }
        },
    });

    sleep(0.2);
}
