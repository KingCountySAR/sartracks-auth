import React, { Component } from 'react';
import { Table, Button } from 'reactstrap';
import Pager from './Pager';
import DebouncedText from '../DebouncedText';
import DataCell from './DataCell';
import DateCell from './DateCell';
import IconCell from './IconCell';

import { SELECT_MODE_NONE, SELECT_MODE_HIDDEN } from '../../redux/data-table';

export { DateCell, IconCell }

const dotted = (obj, path) => path ? path.split('.').reduce((o,i)=>o[i], obj) : <span style={{display:'none'}}>{JSON.stringify(obj)}</span>

const MakeDataRow = React.memo(props => {
  const { d, i, columns, data, onRowToggle } = props
  const cells = columns.map((c, i) => <DataCell key={i} column={c} data={d} value={dotted(d, c.data)} />)
  const className = `${data.opts && data.opts.selectMode > SELECT_MODE_HIDDEN && data.selected && data.selected.indexOf(i) >= 0 ? 'table-primary' : ''}`

  return <tr key={d.id} className={className} onClick={() => data.opts && data.opts.selectMode > SELECT_MODE_NONE && onRowToggle && onRowToggle(i, d)}>{cells}</tr>
})

class DataTable extends Component {
  constructor(props) {
    super(props);
    props.load();
  }

  render() {
    const { columns, data, onRowToggle, setOptions, childRow } = this.props;
    var body = null
    if (!data.failed && data.data.length) {
      body = data.data.map((d, i) => {
                        const ChildRow = childRow && data.selected && data.selected[0] === i ? childRow : null;
                        return {
                            row: <MakeDataRow key={d.id} {...{ d, i, columns, data, onRowToggle }} />,
                        child: ChildRow ? [<tr key={'child-' + d.id}><td className='dt-child' colSpan={columns.length}><ChildRow row={d} /></td></tr>,<tr key={'child-odd-' + d.id}></tr>] : null
                        }})
                        .reduce((prev,cur) => { prev.push(cur.row); cur.child && prev.push(cur.child); return prev }, [])
    } else {
      body =  <tr><td colSpan={columns.length}>
                <div className='d-flex align-items-center'>{data.failed
                  ? <div className='text-danger d-flex flex-grow justify-content-center align-items-center'>Failure getting data.<Button color="link" onClick={() => this.props.load()}>retry</Button></div>
                  : <div className='d-flex flex-grow justify-content-center'>0 rows returned</div>}
                </div>
              </td></tr>
    }

    return (
      <div>
        <div className='form-inline d-flex flex-row justify-content-between align-items-center mb-2'>
          <div>Show <select className='form-control form-control-sm' value={data.opts.size} onChange={(e) => setOptions({size: parseInt(e.target.value)})}>
              <option>10</option>
              <option>25</option>
              <option>100</option>
            </select> entries</div>
          <div><label>Search:&nbsp;<DebouncedText value={data.opts.search} onChange={(v) => setOptions({search: v, page: 1})} className='form-control form-control-sm' /></label></div>
        </div>
        <div style={{position:'relative'}}>
          {data.loading ? <div style={{position:'absolute', width:'100%', height:'100%'}}>
            <div style={{position:'absolute', width:'100%',height:'100%', backgroundColor: 'white', opacity: .5 }}></div>
            <div style={{position:'absolute', width:'100%',height:'100%', display:'flex', alignItems:'center', justifyContent:'center'}}>
            <i className='fas fa-3x fa-spin fa-spinner'></i>
          </div>
        </div> : null}      
        <Table bordered striped hover={data.opts && data.opts.selectMode > SELECT_MODE_NONE} size="sm">
          <thead>
            <tr>
              {columns.map((c, i) => <th key={i}>{c.title}</th>)}
            </tr>
          </thead>
          <tbody>
            {body}
          </tbody>
        </Table>
        <Pager onPage={p => setOptions({page: p})} total={data.meta ? (data.meta.filteredRows||data.meta.totalRows) : null} {...data.opts} />
      </div>
    </div>
    )
  }
}

export default DataTable