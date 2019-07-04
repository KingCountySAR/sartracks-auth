import React, { Component } from 'react';
import { Button, PaginationItem } from 'reactstrap';

const PageLink = (props) => {
  const { children, ...rest} = props;
  return <Button color="link" className="page-link" {...rest}>{props.children}</Button>
}

export default class Pager extends Component {
  render () {
    const { size, page, total, onPage } = this.props;

    var inner = <PaginationItem disabled><PageLink>???</PageLink></PaginationItem>
    var label = <div></div>
    var leftEnabled = false
    var rightEnabled = false
    var rightValue = 0
    if (typeof total === 'number') {
      const totalPages = Math.ceil(total / size)
      const firstNum = Math.max(1, Math.min(Math.max(page - 2, 1), totalPages - 4));
      const nums = Array.from(new Array(Math.min(totalPages, 5)), (x,i) => i + firstNum)
      label = <span>Showing {((page - 1) * size + 1).toLocaleString()} to {Math.min(page * size, total).toLocaleString()} of {total.toLocaleString()} {total === 1 ? 'entry' : 'entries'}</span>
      inner = nums.map(n => <PaginationItem key={n} className={n === page ? 'active' : ''}><PageLink onClick={() => onPage(n)}>{n}</PageLink></PaginationItem>)
      leftEnabled = page > 1
      rightEnabled = page < totalPages
      rightValue = totalPages
    }

    return (
      <nav aria-label="Table page navigation" style={{marginTop: '-.5rem'}} className='d-flex flex-row justify-content-between align-items-center mb-3'>
        {label}
        <ul className="pagination mb-0">
          <PaginationItem disabled={!leftEnabled}><PageLink onClick={() => onPage(1)}><i className="fas fa-angle-double-left"></i></PageLink></PaginationItem>
          {inner}
          <PaginationItem disabled={!rightEnabled}><PageLink onClick={() => onPage(rightValue)}><i className="fas fa-angle-double-right"></i></PageLink></PaginationItem>
        </ul>
      </nav>
    );
  }
}
