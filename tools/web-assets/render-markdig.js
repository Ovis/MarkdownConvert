import mermaid from 'mermaid';
import katex from 'katex';

async function render() {
  for (const node of document.querySelectorAll('.math')) {
    const source = node.textContent.trim();
    const displayMode = source.startsWith('\\[') && source.endsWith('\\]');
    const expression = source.slice(2, -2);
    katex.render(expression, node, { displayMode, throwOnError: true });
  }
  mermaid.initialize({ startOnLoad: false, securityLevel: 'strict' });
  let index = 0;
  for (const node of document.querySelectorAll('pre.mermaid')) {
    const { svg } = await mermaid.render(`markdig-diagram-${index++}`, node.textContent);
    const wrapper = document.createElement('div');
    wrapper.id = 'diagram';
    wrapper.innerHTML = svg;
    node.replaceWith(wrapper);
  }
}

window.renderReady = render();
