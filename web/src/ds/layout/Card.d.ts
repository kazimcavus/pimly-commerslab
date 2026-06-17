import * as React from 'react';
/**
 * Bordered white surface container. Compose with CardHeader / CardBody.
 * @startingPoint section="Layout" subtitle="Card surface with header/body" viewport="700x260"
 */
export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Apply default padding instead of using CardBody. */
  pad?: boolean;
  children?: React.ReactNode;
}
export interface CardHeaderProps {
  title?: React.ReactNode;
  /** Right-aligned actions (buttons, menus). */
  actions?: React.ReactNode;
  className?: string;
}
export interface CardBodyProps { children?: React.ReactNode; className?: string; }
export declare function Card(props: CardProps): React.JSX.Element;
export declare function CardHeader(props: CardHeaderProps): React.JSX.Element;
export declare function CardBody(props: CardBodyProps): React.JSX.Element;
