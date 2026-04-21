import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    stages: [
        { duration: '1m', target: 20 },
        { duration: '2m', target: 50 },
        { duration: '3m', target: 100 },
        { duration: '2m', target: 100 },
        { duration: '1m', target: 0 }
    ],
};

const BASE_URL = 'http://localhost:5255';
const TEST_USER = 'kevon25';
const TEST_PASSWORD = 'Password123!';

export function setup() {
    const loginRes = http.post(
        `${BASE_URL}/api/v1/auth/login`,
        JSON.stringify({
            UsernameOrEmail: TEST_USER,
            Password: TEST_PASSWORD,
        }),
        {
            headers: { 'Content-Type': 'application/json' },
        }
    );

    check(loginRes, {
        'setup login status 200': (r) => r.status === 200,
    });

    return {
        token: JSON.parse(loginRes.body).data.accessToken,
    };
}

export default function (data) {
    const token = data.token;

    let cursor = null;
    const maxPages = Math.floor(Math.random() * 5) + 1;

    for (let i = 0; i < maxPages; i++) {
        let url = `${BASE_URL}/api/v1/cars`;

        if (cursor) {
            url += `?cursor=${cursor}`;
        }

        const res = http.get(url, {
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
                    return false
                }
            },
        });

        try {
            const body = JSON.parse(res.body);
            cursor = body?.data?.nextCursor || null;

            if (!cursor) {
                break;
            }
        } catch {
            break;
        }

    }

}
