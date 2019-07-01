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
    props.doLoadAccount(props.row.id, props.row.attributes.memberId)
  }

  render() {
    const { row, accounts, token } = this.props;
    const logins = ((accounts.details || {})[row.id]||{}).logins;
    const member = ((accounts.details || {})[row.id]||{}).member;
    return <div className='d-flex flex-row'>
      <div style={{paddingLeft: '1rem', paddingRight: '1rem'}}>
        <h6 style={{marginLeft: '-1rem'}}>External Logins</h6>
        {logins
          ? logins.length
            ? logins.map(login => <div key={login.id}><i style={{color: login.meta.color}} className={`fab ${login.meta.icon}`} /> {login.attributes.displayName}</div>)
            : <div><i>No external logins</i></div>
          : <div><i className="fas fa-spin fa-spinner"></i></div>}
      </div>
      <div style={{paddingLeft: '1rem', paddingRight: '1rem'}}>
        <h6 style={{marginLeft: '-1rem'}}>Member Information</h6>
        {member
          ? (member.meta||{}).notAMember
            ? <div><i>No member found</i></div>
            : <div className='d-flex flex-row'>
                <img className='badge-photo' alt='Member headshot' src={`${window.reactConfig.apis.data.url}/members/${member.id}/photo?access_token=${token}`} />
                <div>
                  <div className={'wacbar wac_' + member.attributes.wacLevel}>{member.attributes.wacLevel}</div>
                  <div>{member.attributes.name}</div>
                  <div>ID#: {member.attributes.workerNumber}</div>
                </div>
              </div>
          : <div><i className='fas fa-spin fa-spinner'></i></div>}
      </div>
      <div style={{paddingLeft: '1rem', paddingRight: '1rem'}}>
        <h6 style={{marginLeft: '-1rem'}}>Unit Membership</h6>
        {member
          ? (member.meta||{}).notAMember
            ? null
            : <div>
                {member.attributes.units.map(u => <div key={u.unit.id}>{u.unit.name} - {u.status}</div>)}
              </div>
          : <div><i className='fas fa-spin fa-spinner'></i></div>}
      </div>
    </div>
  }
}
const ConnectedChildRow = connect(
  (store) => {
    return {
      accounts: store.accounts,
      token: store.oidc.user.access_token
    }
  },
  (dispatch, ownProps) => {
    return {
      doLoadAccount: (userId, memberId) => dispatch(actions.loadAccountDetails(userId, memberId))
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