'use server';

import { cookies } from 'next/headers';

const baseUrl = 'http://localhost:5253/api';
const sessionKey = '__psession__';
export type ActionResponse<T> = {
    success: boolean,
    data?: T,
    error?: any
};

export async function actionPost<T>(endpoint: string, req: any) : Promise<ActionResponse<T>> {
    try {
        const headerAuth: any = {};
        if (cookies().has(sessionKey)) {
            const token = JSON.parse(cookies().get(sessionKey)?.value || "{}").token || "";
            headerAuth['Authorization'] = `bearer ${token}`;
        }
        const result = await fetch(`${baseUrl}/${endpoint.startsWith('/') ? endpoint.substring(1) : endpoint}`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
                ...headerAuth
            },
            body: JSON.stringify(req)
        });
        const data = await result.json();
        return {
            success: result.ok,
            data: result.ok ? data : null,
            error: result.ok ? null : data.data || 'Something wrong'
        };
    } catch (error: any) {
        return {
            success: false,
            error: error.message
        }
    }
}