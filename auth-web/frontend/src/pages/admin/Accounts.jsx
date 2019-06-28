import React, { Component } from 'react';
import { connect } from 'react-redux'
import { Link } from 'react-router-dom';
//import { Button } from 'reactstrap';
import DataTable, { DateCell, IconCell } from '../../components/table/DataTable';
import { actions } from '../../redux/accounts';

const UsernameCell = React.memo((props) => props.value === '@' ? <i>External Login</i> : props.value)

const ActionCell = React.memo((props) => [
  // <Button key='1' color='link'><i className={`fas fa-${props.data.attributes.isLocked ? 'un' : ''}lock`}></i></Button>,
  <Link key='2' to={`/admin/accounts/${props.data.id}`}><i className="fas fa-id-card"></i></Link>
])

class ChildRow extends Component {
  constructor(props) {
    super(props)
    props.doLoadAccount(props.row.id)
  }

  render() {
    const { row, accounts } = this.props;
    const logins = ((accounts.details || {})[row.id]||{}).logins;
    return <div className='d-flex flex-row'>
      <div style={{paddingLeft: '1rem', paddingRight: '1rem'}}>
        <h6 style={{marginLeft: '-1rem'}}>External Logins</h6>
        {logins
          ? logins.length
            ? logins.map(login => <div key={login.id}><i style={{color: login.meta.color}} className={`fab ${login.meta.icon}`} /> {login.attributes.displayName}</div>)
            : <div><i>No external logins</i></div>
          : <div><i className="fas fa-spin fa-spinner"></i></div>}
      </div>
      <div>
        <h6>Member Information</h6>
      </div>
    </div>
  }
}
const ConnectedChildRow = connect(
  (store) => {
    return {
      accounts: store.accounts
    }
  },
  (dispatch, ownProps) => {
    return {
      doLoadAccount: userId => dispatch(actions.loadAccountDetails(userId))
    }
  }
  )(ChildRow)

class AccountsPage extends Component {
  render() {
    const columns = [
      { title: 'Last Name', data: 'attributes.lastName' },
      { title: 'Name', data: 'attributes.name' },
      { title: 'Email', data: 'attributes.email' },
      { title: 'User Name', data: 'attributes.userName', render: UsernameCell },
      { title: 'Locked?', data: 'attributes.isLocked', className: 'text-center', render: { component: IconCell, faSet: 'fas', onIcon: 'fa-lock' }},
      { title: 'Last Login', data: 'attributes.lastLogin', render: { component: DateCell, tz: 'America/Los_Angeles', emptyText: 'Never' } },
      { render: ActionCell, className: 'dt-actions' }
    ]

    const { doLoad, doTableOpts, doRowToggle, dataTable } = this.props;

    return (<div>
      <h2>Accounts</h2>
      <div>
        <DataTable load={doLoad} setOptions={doTableOpts} onRowToggle={doRowToggle} columns={columns} data={dataTable} childRow={ConnectedChildRow} />
      </div>
    </div>)
  }
}

const storeToProps = (store) => {
  return {
    dataTable: store.accounts.list
  }
}

const dispatchToProps = (dispatch, ownProps) => {
  return {
    doLoad: () => dispatch(actions.load()),
    doTableOpts: opts => dispatch(actions.setOptions(opts)),
    doRowToggle: (index, row) => dispatch(actions.toggleRow(index, row))
  }
}

export default connect(storeToProps, dispatchToProps)(AccountsPage)