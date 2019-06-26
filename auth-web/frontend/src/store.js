import { createStore, applyMiddleware, combineReducers } from 'redux'
import thunkMiddleware from 'redux-thunk'
import { createLogger } from 'redux-logger'

import oidc from './reducers/oidc-reducer'

const defaultState = {
  oidc: { signedIn: false, user: null },
}

const middleware = [
  thunkMiddleware
]

if(process.env.NODE_ENV === 'development' || (localStorage && localStorage.showLogging)) {
	const loggerMiddleware = createLogger({ collapsed: true });
	middleware.push(loggerMiddleware);
}

const rootReducer = combineReducers({
  oidc
})

const store = createStore(
  rootReducer,
  defaultState,
  applyMiddleware(...middleware)
)

export default store