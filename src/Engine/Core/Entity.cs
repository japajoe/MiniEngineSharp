namespace MiniEngine.Core
{
    public class Entity
    {
        protected Transform m_transform;
        protected bool m_isActive;
        protected string m_name;

        public Transform transform
        {
            get => m_transform;
        }

        public bool isActive
        {
            get => m_isActive;
            set => m_isActive = value;
        }

        public string name
        {
            get => m_name;
            set => m_name = value;
        }

        public Entity()
        {
            m_transform = new Transform();
            m_isActive = true;
            m_name = string.Empty;
        }
    }
}