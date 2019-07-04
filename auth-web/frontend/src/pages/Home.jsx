import React, { Component } from 'react';
import AuthRoute from '../components/auth/AuthRoute';
import { connect } from 'react-redux';
import { actions } from '../redux/me';
import userManager from '../user-manager';
import { ActionsFactory } from '../redux/oidc';

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

    return user
        ? <AuthRoute component={UserHome} denied='' loading='' {...this.props} />
      :
      <div>        
        <h1>Hello, world!</h1>
        <p>Welcome to your new single-page application, built with:</p>
        <ul>
          <li><a href='https://get.asp.net/'>ASP.NET Core</a> and <a href='https://msdn.microsoft.com/en-us/library/67ef8sbd.aspx'>C#</a> for cross-platform server-side code</li>
          <li><a href='https://facebook.github.io/react/'>React</a> for client-side code</li>
          <li><a href='http://getbootstrap.com/'>Bootstrap</a> for layout and styling</li>
        </ul>
        <p>To help you get started, we have also set up:</p>
        <ul>
          <li><strong>Client-side navigation</strong>. For example, click <em>Counter</em> then <em>Back</em> to return here.</li>
          <li><strong>Development server integration</strong>. In development mode, the development server from <code>create-react-app</code> runs in the background automatically, so your client-side resources are dynamically built on demand and the page refreshes when you modify any file.</li>
          <li><strong>Efficient production builds</strong>. In production mode, development-time features are disabled, and your <code>dotnet publish</code> configuration produces minified, efficiently bundled JavaScript files.</li>
        </ul>
        <p>The <code>ClientApp</code> subdirectory is a standard React application based on the <code>create-react-app</code> template. If you open a command prompt in that directory, you can run <code>npm</code> commands such as <code>npm test</code> or <code>npm install</code>.</p>
      </div>
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