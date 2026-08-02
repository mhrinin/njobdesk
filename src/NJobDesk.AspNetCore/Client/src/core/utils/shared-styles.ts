import { css } from "lit";

export const popoverMenuStyles = css`
  .menu {
    display: flex;
    flex-direction: column;
    background-color: var(--uui-color-surface);
    border-radius: var(--uui-border-radius);
    box-shadow: var(--uui-shadow-depth-3);
    padding: var(--uui-size-space-2);
  }
`;

export const searchInputIconStyles = css`
  uui-icon[slot="prepend"] {
    display: flex;
    align-items: center;
    height: 100%;
    margin: 0 var(--uui-size-space-2) 0 var(--uui-size-space-3);
    color: var(--uui-color-text-alt);
  }
`;
