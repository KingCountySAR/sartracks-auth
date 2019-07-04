import React, { Component } from 'react';
import AuthRoute from '../components/auth/AuthRoute';
import { connect } from 'react-redux';
import { actions } from '../redux/me';
import userManager from '../user-manager';
import { ActionsFactory } from '../redux/oidc';
import FacebookPage from '../components/facebook-page';
import './Home.css';

const oidcActions = ActionsFactory(userManager);

class UserHome extends Component {
  constructor(props) {
    super(props)
    props.loadApps();
  }

  render() {
    const { me } = this.props
    return <div>
      <div>Here's a list of applications you can use:</div>
      {me.apps.loading
        ? <div><i className='fas fa-spin fa-spinner'></i> Loading</div>
        : me.apps.failed
          ? <div>failed</div>
          : me.apps.data && me.apps.data.length
            ? <table className='app-list'>
              <tbody>
                {me.apps.data.map(a => <tr key={a.id}><td><a href={a.attributes.url}>{a.attributes.name}</a></td><td>{a.attributes.description}</td></tr>)}                
              </tbody>
            </table>
            : <div>No applications available</div>
      }
      </div>
  }
}

class Home extends Component {
  static displayName = Home.name;

  constructor(props) {
    super(props)
    props.preloadUser();
  }

  render () {
    const { user } = this.props;

    return (
    <React.Fragment>
      <div className='row'>
        <h2 className='col'>KCSARA Member Portal</h2>
      </div>
      {user
        ? <AuthRoute component={UserHome} denied='' loading='' {...this.props} />
        :<div className='row' style={{flex: '1 1 auto'}}>
          <div className='col-lg'>
            <p>This site provides members of <a href="http://kingcountysar.org">King County Search and Rescue</a> access to various member resources. In order to access these resources you must log in.</p>
          </div>
          <div className='col-lg-5' style={{flex:'1 1 auto'}} ref={el => this.facebookColumn = el}>
            <FacebookPage />
          </div>
          <div id="fb-root"></div>
        </div>
      }
    </React.Fragment>)
  }
}

const storeToProps = (store) => {
  return {
    user: store.oidc.user,
    me: store.me
  }
}

const dispatchToProps = (dispatch, ownProps) => {
  return {
    preloadUser: () => dispatch(oidcActions.preloadUser),
    loadApps: () => dispatch(actions.loadApplications())
  }
}

export default connect(storeToProps, dispatchToProps)(Home);