using System;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Core
{
    public enum ParticleType
    {
        Dust,       // Bụi di chuyển (màu xám/nâu)
        Aura,       // Aura tu luyện (glowing bay lên)
        Ember,      // Hỏa (phát sáng, bay bập bùng)
        Leaf,       // Mộc (lá cây, xoay nhẹ)
        Snow,       // Băng (tinh thể tuyết bay ngang)
        Spark,      // Tia sét/xẹt lửa cơ quan (None/Neutral)
        Burst       // Nổ năng lượng khi quái chết / đột phá
    }

    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Lifetime;
        public float Elapsed;
        public float Rotation;
        public float RotationSpeed;
        public ParticleType Type;
        
        public bool Active => Elapsed < Lifetime;

        public Particle(Vector2 position, Vector2 velocity, Color color, float size, float lifetime, ParticleType type)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Lifetime = lifetime;
            Elapsed = 0f;
            Type = type;

            var random = new Random();
            Rotation = (float)(random.NextDouble() * Math.PI * 2);
            RotationSpeed = (float)(random.NextDouble() * 4 - 2);
        }

        public void Update(float deltaTime)
        {
            Elapsed += deltaTime;
            
            // Cập nhật vị trí dựa theo loại particle
            switch (Type)
            {
                case ParticleType.Dust:
                    // Bụi di chuyển chậm dần và bay lên nhẹ
                    Velocity *= 0.92f;
                    Velocity.Y -= 5f * deltaTime;
                    Position += Velocity * deltaTime;
                    break;

                case ParticleType.Aura:
                    // Aura minh tưởng bay zic-zac đi lên
                    Velocity.X = (float)Math.Sin(Elapsed * 8f) * 15f;
                    Velocity.Y = -25f; // Đi lên đều
                    Position += Velocity * deltaTime;
                    break;

                case ParticleType.Ember:
                    // Hỏa linh bay bập bùng chậm dần
                    Velocity.Y -= 15f * deltaTime; // Nhẹ hơn không khí
                    Velocity.X *= 0.95f;
                    Position += Velocity * deltaTime;
                    break;

                case ParticleType.Leaf:
                    // Lá mộc rơi chao lượn trái phải
                    Velocity.X = (float)Math.Sin(Elapsed * 4f) * 30f;
                    Velocity.Y += 5f * deltaTime; // Rơi nhẹ
                    Position += Velocity * deltaTime;
                    Rotation += RotationSpeed * deltaTime;
                    break;

                case ParticleType.Snow:
                    // Bông tuyết băng trôi lững lờ, giảm dần tốc độ rơi
                    Velocity.Y += 2f * deltaTime;
                    Position += Velocity * deltaTime;
                    Rotation += RotationSpeed * 0.5f * deltaTime;
                    break;

                case ParticleType.Spark:
                    // Tia xẹt cơ quan bay thẳng, ma sát cao
                    Velocity *= 0.9f;
                    Position += Velocity * deltaTime;
                    break;

                case ParticleType.Burst:
                    // Vụ nổ đột phá tỏa tròn cực mạnh
                    Velocity *= 0.95f;
                    Position += Velocity * deltaTime;
                    Rotation += RotationSpeed * deltaTime;
                    break;
            }
        }
    }
}
