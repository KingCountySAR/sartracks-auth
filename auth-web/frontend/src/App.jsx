import './App.css';
import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import Home from './pages/Home';
import LoginCallback from './components/auth/LoginCallback';
import AdminAccounts from './pages/admin/Accounts';
import AdminAccount from './pages/admin/Account';
import AuthRoute from './components/auth/AuthRoute';

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={Home} />
        <Route exact path='/loggedIn' component={LoginCallback} />
        <AuthRoute exact path='/admin/accounts' component={AdminAccounts} />
        <AuthRoute exact path='/admin/accounts/:id' component={AdminAccount} />
      </Layout>
    );
  }
}
