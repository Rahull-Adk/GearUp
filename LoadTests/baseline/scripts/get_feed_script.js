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

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5255';
const TEST_USER = __ENV.TEST_USER || 'ruby38';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || 'Password123!';

function parseJsonSafe(rawBody) {
    if (!rawBody) return null;

    try {
        return JSON.parse(rawBody);
    } catch {
        return null;
    }
}

function extractAccessToken(body) {
    if (!body || typeof body !== 'object') {
        return null;
    }

    const data = body.data || body.Data || null;
    return (
        data?.accessToken ||
        data?.AccessToken ||
        body.accessToken ||
        body.AccessToken ||
        null
    );
}

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

    const body = parseJsonSafe(loginRes.body);
    const token = extractAccessToken(body);

    check(loginRes, {
        'setup login status 200': (r) => r.status === 200,
        'setup login has access token': () => !!token,
    });

    if (loginRes.status !== 200 || !token) {
        const message = body?.message || body?.Message || 'Unknown login error';
        const bodySnippet = String(loginRes.body || '').slice(0, 400);
        throw new Error(`Login setup failed (status=${loginRes.status}, message=${message}). Response: ${bodySnippet}`);
    }

    return {
        token,
    };
}

export default function (data) {
    const token = data.token;

    let cursor = null;
    const maxPages = Math.floor(Math.random() * 5) + 1;

    for (let i = 0; i < maxPages; i++) {
        let url = `${BASE_URL}/api/v1/posts`;

        if (cursor) {
            url += `?cursor=${encodeURIComponent(cursor)}`;
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
            cursor = body?.data?.nextCursor || body?.Data?.NextCursor || null;

            if (!cursor) {
                break;
            }
        } catch {
            break;
        }

    }

}
