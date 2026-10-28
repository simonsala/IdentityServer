import { createClient } from 'redis';

export type SessionUser = {
  name?: string
  givenName?: string
}

export type SessionTokens = {
  idToken: string
  accessToken: string
  refreshToken?: string
  expiresAt: number,
}

export type SessionData = {
  user: SessionUser
  tokens: SessionTokens
}

function createRedisClient() {
  const client = createClient({
    url: process.env.REDIS_URL
  });
  client.on('error', (err) => console.error('Redis Client Error', err));
  return client;
}

async function getNextId(entityName: string) {
    const client = createRedisClient();
    await client.connect();
    
    // Redis key designated for counting (e.g., "counter:users")
    const counterKey = `counter:${entityName}`;
    
    // INCR automatically starts at 1 if the key does not exist
    const nextId = await client.incr(counterKey);
    return nextId;
}

export async function createNewSession(sessionData: SessionData): Promise<number | undefined> {
    try {
        const redisClient = createRedisClient();
        const sessionId = await getNextId('session');
        

        await redisClient.connect();
        await redisClient.json.set(`session:${sessionId}`, '$', sessionData);
        
        console.log(`Successfully created session:`, sessionData);
        return sessionId; 
    } catch (error) {
        console.error('Failed to create session:', error);
    }
}

export async function getSession(sessionId: number) {
    const redisClient = createRedisClient();
    await redisClient.connect();
    const sessionData = await redisClient.json.get(`session:${sessionId}`);
    return sessionData;
}