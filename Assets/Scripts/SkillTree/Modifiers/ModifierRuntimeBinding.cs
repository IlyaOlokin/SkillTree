using System;

namespace SkillTree
{
    public interface IModifierRuntimeBinding
    {
        void Bind();
        void Unbind();
    }

    public sealed class DelegateModifierRuntimeBinding : IModifierRuntimeBinding
    {
        private readonly Action _bind;
        private readonly Action _unbind;

        public DelegateModifierRuntimeBinding(Action bind, Action unbind)
        {
            _bind = bind;
            _unbind = unbind;
        }

        public void Bind()
        {
            _bind?.Invoke();
        }

        public void Unbind()
        {
            _unbind?.Invoke();
        }
    }
}
