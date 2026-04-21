import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    scenarios: {
        search_load: {
            executor: 'ramping-vus',
            stages: [
                { duration: '1m', target: 20 },
                { duration: '2m', target: 50 },
                { duration: '3m', target: 100 },
                { duration: '2m', target: 100 },
                { duration: '1m', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01'],
    },
    cloud: {
        projectID: 7110349,
        name: 'Test (28/03/2026-13:05:29)'

    }
};

const BASE_URL = 'http://localhost:5255';
const TEST_USER = 'kevon25';
const TEST_PASSWORD = 'Password123!';

const queries = ['toyota', 'bmw', 'honda', 'suv', ''];
const colors = ['red', 'black', 'white', ''];

const priceRanges = [
    [0, 5000],
    [5000, 15000],
    [15000, 30000],
];

const sortOptions = [
    { by: 'price', order: 'asc' },
    { by: 'price', order: 'desc' },
    { by: 'createdAt', order: 'desc' },
];

export function setup() {
    const res = http.post(
        `${BASE_URL}/api/v1/auth/login`,
        JSON.stringify({
            UsernameOrEmail: TEST_USER,
            Password: TEST_PASSWORD,
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );

    if (res.status !== 200) throw new Error('Login failed');

    return {
        token: JSON.parse(res.body).data.accessToken,
    };
}

export default function (data) {
    const token = data.token;

    const q = randomItem(queries);
    const color = randomItem(colors);
    const [minPrice, maxPrice] = randomItem(priceRanges);
    const sort = randomItem(sortOptions);

    let cursor = null;

    for (let i = 0; i < 2; i++) {
        let url = `${BASE_URL}/api/v1/cars/search?`;

        if (q) url += `Query=${q}&`;
        if (color) url += `Color=${color}&`;

        url += `MinPrice=${minPrice}&MaxPrice=${maxPrice}&`;
        url += `SortBy=${sort.by}&SortOrder=${sort.order}&`;

        if (cursor) {
            url += `cursor=${cursor}`;
        }

        const res = http.get(url, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
            tags: {
                name: 'GET /cars/search',
            },
        });

        check(res, {
            'status 200': (r) => r.status === 200,
            'fast enough (<800ms)': (r) => r.timings.duration < 800,
        });

        try {
            const body = JSON.parse(res.body);
            cursor = body?.data?.nextCursor;
        } catch {
            cursor = null;
        }

        sleep(0.2);
    }
}
function randomItem(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
}
