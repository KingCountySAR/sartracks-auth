import React, { Component } from 'react';
import { connect } from 'react-redux'
import { Link } from 'react-router-dom';
//import { Button } from 'reactstrap';
import DataTable, { DateCell, IconCell } from '../../components/table/DataTable';
import { actions } from '../../redux/accounts';
import MemberProfileCard from '../../components/account/member-profile-card';
import MemberUnitsCard from '../../components/account/member-units-card';
import AccountLoginsCard from '../../components/account/account-logins-card';

const UsernameCell = React.memo((props) => props.value === '@' ? <i>External Login</i> : props.value)

const ActionCell = React.memo((props) => [
  // <Button key='1' color='link'><i className={`fas fa-${props.data.attributes.isLocked ? 'un' : ''}lock`}></i></Button>,
  <Link key='2' to={`/admin/accounts/${props.data.id}`}><i className="fas fa-id-card"></i></Link>
])

class ChildRow extends Component {
  constructor(props) {
    super(props)
    props.doLoadDetails(props.row.id, props.row.attributes.memberId)
  }

  render() {
    const { accounts, token } = this.props;
    const member = accounts.details.member;
    return <div className='d-flex flex-row'>
      <AccountLoginsCard logins={accounts.details.logins} />
      <MemberProfileCard member={member} token={token} />
      <MemberUnitsCard member={member} />
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
      doLoadDetails: (userId, memberId) => dispatch(actions.loadAccountDetails(userId, memberId))
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